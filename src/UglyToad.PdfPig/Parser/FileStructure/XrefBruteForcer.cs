namespace UglyToad.PdfPig.Parser.FileStructure;

using System.Buffers;
using Core;
using Logging;
using Tokenization.Scanner;
using Tokens;
using Filters;

internal static class XrefBruteForcer
{
    // The keywords the scan looks for, packed into the low bytes of a 64-bit window with the
    // newest byte lowest, and the masks that select just their length. Each keyword ends in a
    // byte the others do not, so the byte just read tells which one comparison to make.
    private static readonly ulong ObjTail = Tail(" obj");
    private static readonly ulong XrefTail = Tail(" xref");
    private static readonly ulong XrefStreamTail = Tail("/XRef");
    private static readonly ulong TrailerTail = Tail("trailer ");
    private const ulong FourByteMask = 0xFFFFFFFFUL;
    private const ulong FiveByteMask = 0xFFFFFFFFFFUL;

    /// <summary>
    /// The input is read in blocks of this size and scanned from the array, rather than a byte at
    /// a time through the input's interface.
    /// </summary>
    private const int BlockSize = 64 * 1024;

    public static Result FindAllXrefsInFileOrder(
        IInputBytes bytes,
        ISeekableTokenScanner scanner,
        IFilterProvider filterProvider,
        ILog log)
    {
        var results = new List<IXrefSection>();

        // Guard against circular references; only read xref at each offset once
        var xrefOffsetSeen = new HashSet<long>();

        var bruteForceObjPositions = new Dictionary<IndirectReference, XrefLocation>();

        DictionaryToken? trailer = null;

        // The last eight bytes seen, whitespace normalized to a space, newest byte lowest. A
        // keyword has been read when the low bytes equal its packed form; before enough bytes
        // are in, the zeros above them cannot equal any keyword byte.
        var window = 0UL;

        // The number being read: its value, how many digits it has, and whether it outgrew a
        // long, in which case it is not a number this scan can use, as long.TryParse would say.
        var number = 0L;
        var numberLength = 0;
        var numberOverflowed = false;

        var inNum = false;
        var lastWhitespace = false;
        var inComment = false;

        var numericsQueue = new long[2];
        var positionsQueue = new long[2];

        long? lastObjPosition = null;

        void ClearQueues()
        {
            numericsQueue[0] = 0;
            numericsQueue[1] = 0;
            positionsQueue[0] = 0;
            positionsQueue[1] = 0;
        }

        // The offset is the position the input reports while on the byte after the number.
        void AddQueues(long num, long currentOffset)
        {
            numericsQueue[0] = numericsQueue[1];
            numericsQueue[1] = num;
            positionsQueue[0] = positionsQueue[1];
            positionsQueue[1] = currentOffset - numberLength - 1;
        }

        void EndNumber(long currentOffset)
        {
            if (inNum && numberLength > 0 && !numberOverflowed)
            {
                AddQueues(number, currentOffset);
            }

            number = 0;
            numberLength = 0;
            numberOverflowed = false;
        }

        // The block has one byte of room in front of the data read into it, for the byte that
        // the previous read ended on: it is visited only once the next read has shown that it
        // was not the last byte of the input. That way the scan needs no length from the input,
        // which a buffering stream does not know until it has been read to the end, and it
        // leaves the last byte unvisited like the byte-wise loop it replaces. No keyword that
        // ends on the last byte could yield anything: an object with nothing after it, a table
        // or a trailer with nothing left to read.
        var block = ArrayPool<byte>.Shared.Rent(BlockSize + 1);

        try
        {
            // Positions are computed from the index: the input reports an offset one past the
            // byte it is on. Where a keyword hands the input to another parser, the scan resumes
            // from wherever that left the input, as the byte-wise loop did.
            var next = 0L;
            var hasPending = false;

            while (true)
            {
                bytes.Seek(next);

                var read = bytes.Read(block.AsSpan(1, BlockSize));
                if (read <= 0)
                {
                    break;
                }

                var start = hasPending ? 0 : 1;
                var end = 1 + read;
                var firstIndex = hasPending ? next - 1 : next;
                next += read;

                var resumed = false;

                for (var k = start; k < end - 1; k++)
                {
                    var currentOffset = firstIndex + (k - start) + 1;
                    var current = block[k];

                    if (current == '%')
                    {
                        inComment = true;

                        EndNumber(currentOffset);

                        inNum = false;
                        lastWhitespace = false;
                    }

                    if (ReadHelper.IsWhitespace(current))
                    {
                        if (ReadHelper.IsEndOfLine(current))
                        {
                            inComment = false;
                        }

                        // Normalize whitespace
                        window = (window << 8) | (byte)' ';

                        EndNumber(currentOffset);

                        lastWhitespace = true;
                        inNum = false;
                    }
                    else
                    {
                        window = (window << 8) | current;

                        if (!inComment && ReadHelper.IsDigit(current) && (inNum || lastWhitespace))
                        {
                            inNum = true;

                            var digit = current - '0';
                            if (numberOverflowed || number > (long.MaxValue - digit) / 10)
                            {
                                numberOverflowed = true;
                            }
                            else
                            {
                                number = (number * 10) + digit;
                            }

                            numberLength++;
                        }
                        else
                        {
                            inNum = false;
                            number = 0;
                            numberLength = 0;
                            numberOverflowed = false;
                        }

                        lastWhitespace = false;
                    }

                    // The byte just added is the last of the keyword, if any, and the four
                    // keywords end in three different bytes.
                    var lastByte = (byte)window;

                    if (lastByte == 'j')
                    {
                        if ((window & FourByteMask) == ObjTail && numericsQueue[0] > 0)
                        {
                            bruteForceObjPositions[new IndirectReference(numericsQueue[0], (int)numericsQueue[1])] = XrefLocation.File(positionsQueue[0]);

                            lastObjPosition = positionsQueue[0];

                            ClearQueues();
                        }
                    }
                    else if (lastByte == 'f')
                    {
                        var tail = window & FiveByteMask;

                        if (tail == XrefTail)
                        {
                            ClearQueues();

                            var potentialTableOffset = currentOffset - 4;

                            if (xrefOffsetSeen.Contains(potentialTableOffset))
                            {
                                log.Debug($"Skipping circular xref reference at {potentialTableOffset}");
                                continue;
                            }

                            xrefOffsetSeen.Add(potentialTableOffset);

                            var table = XrefTableParser.TryReadTableAtOffset(
                                new FileHeaderOffset(0),
                                potentialTableOffset,
                                bytes,
                                scanner,
                                log);

                            if (table != null)
                            {
                                results.Add(table);
                            }
                            else
                            {
                                log.Warn(
                                    $"Found a table at {potentialTableOffset} but couldn't parse it.");
                            }

                            // TryReadTableAtOffset seeks the shared input and does not restore the
                            // position (including on failure). The scan resumes right after the
                            // keyword — otherwise a failed parse can skip past later keywords such
                            // as a recoverable 'trailer' dictionary.
                            next = currentOffset;
                            resumed = true;
                            break;
                        }

                        if (tail == XrefStreamTail)
                        {
                            ClearQueues();

                            if (lastObjPosition is not long offset)
                            {
                                log.Error("Found an /XRef without having encountered an object first");
                                continue;
                            }

                            if (xrefOffsetSeen.Contains(offset))
                            {
                                log.Debug($"Skipping circular /XRef reference at {offset}");
                                continue;
                            }

                            xrefOffsetSeen.Add(offset);

                            var stream = XrefStreamParser.TryReadStreamAtOffset(
                                new FileHeaderOffset(0),
                                offset,
                                bytes,
                                scanner,
                                filterProvider,
                                log);

                            if (stream != null)
                            {
                                results.Add(stream);
                            }

                            // Same position-preservation as the table branch above.
                            next = currentOffset;
                            resumed = true;
                            break;
                        }
                    }
                    else if (lastByte == ' ' && window == TrailerTail)
                    {
                        ClearQueues();

                        // Ensure the scanner reads from the byte scan's current position —
                        // a preceding failed table/stream parse may have moved it elsewhere.
                        scanner.Seek(currentOffset);

                        // Grab the last trailer dictionary as backup in case we find no valid xrefs.
                        if (scanner.TryReadToken(out DictionaryToken trailerDict))
                        {
                            trailer = trailerDict;
                        }

                        // The scan goes on from wherever the dictionary ended, as it always has.
                        next = bytes.CurrentOffset;
                        resumed = true;
                        break;
                    }
                }

                if (resumed)
                {
                    hasPending = false;
                }
                else
                {
                    block[0] = block[end - 1];
                    hasPending = true;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(block);
        }

        return new Result(
            results,
            bruteForceObjPositions,
            trailer);
    }

    /// <summary>The keyword packed into the low bytes of a window, its last byte lowest.</summary>
    private static ulong Tail(string keyword)
    {
        var value = 0UL;

        foreach (var c in keyword)
        {
            value = (value << 8) | (byte)c;
        }

        return value;
    }

    public class Result(
        IReadOnlyList<IXrefSection> xRefParts,
        IReadOnlyDictionary<IndirectReference, XrefLocation> objectOffsets,
        DictionaryToken? lastTrailer)
    {
        public IReadOnlyList<IXrefSection> XRefParts { get; } = xRefParts;
        public IReadOnlyDictionary<IndirectReference, XrefLocation> ObjectOffsets { get; } = objectOffsets;
        public DictionaryToken? LastTrailer { get; } = lastTrailer;
    }
}
