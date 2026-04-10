namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Logging;
using System.Globalization;
using Tokenization.Scanner;
using Tokens;
using Util;

internal static class XrefBruteForcer
{
    public static Result FindAllXrefsInFileOrder(
        IInputBytes bytes,
        ISeekableTokenScanner scanner,
        ILog log)
    {
        var results = new List<IXrefSection>();

        // Guard against circular references; only read xref at each offset once
        var xrefOffsetSeen = new HashSet<long>();

        var bruteForceObjPositions = new Dictionary<IndirectReference, XrefLocation>();

        DictionaryToken? trailer = null;

        bytes.Seek(0);

        var buffer = new CircularByteBuffer(10);

        var numberByteBuffer = new List<byte>();

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

        void AddQueues(long num)
        {
            numericsQueue[0] = numericsQueue[1];
            numericsQueue[1] = num;

            positionsQueue[0] = positionsQueue[1];
            positionsQueue[1] = bytes.CurrentOffset - numberByteBuffer.Count - 1;
        }

        // search for xref tables and /XRef stream types, record all object positions.
        while (bytes.MoveNext() && !bytes.IsAtEnd())
        {
            if (bytes.CurrentByte == '%')
            {
                inComment = true;

                if (inNum && numberByteBuffer.Count > 0)
                {
                    var num = OtherEncodings.BytesAsLatin1String(numberByteBuffer.ToArray());
                    if (long.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numLong))
                    {
                        AddQueues(numLong);
                    }

                    numberByteBuffer.Clear();
                }
                
                inNum = false;
                lastWhitespace = false;

            }

            if (ReadHelper.IsWhitespace(bytes.CurrentByte))
            {
                if (ReadHelper.IsEndOfLine(bytes.CurrentByte))
                {
                    inComment = false;
                }

                // Normalize whitespace
                buffer.Add((byte)' ');

                if (inNum && numberByteBuffer.Count > 0)
                {
                    var num = OtherEncodings.BytesAsLatin1String(numberByteBuffer.ToArray());
                    if (long.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numLong))
                    {
                        AddQueues(numLong);
                    }

                    numberByteBuffer.Clear();
                }

                lastWhitespace = true;
                inNum = false;
            }
            else
            {
                buffer.Add(bytes.CurrentByte);

                if (!inComment && ReadHelper.IsDigit(bytes.CurrentByte) && (inNum || lastWhitespace))
                {
                    inNum = true;
                    numberByteBuffer.Add(bytes.CurrentByte);
                }
                else
                {
                    inNum = false;
                    numberByteBuffer.Clear();
                }

                lastWhitespace = false;
            }

            if (buffer.EndsWith(" obj") && numericsQueue[0] > 0)
            {
                bruteForceObjPositions[new IndirectReference(numericsQueue[0], (int)numericsQueue[1])] = XrefLocation.File(positionsQueue[0]);

                lastObjPosition = positionsQueue[0];

                ClearQueues();
            }
            else if (buffer.EndsWith(" xref"))
            {
                ClearQueues();

                var potentialTableOffset = bytes.CurrentOffset - 4;

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
            }
            else if (buffer.EndsWith("/XRef"))
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
                    log);

                if (stream != null)
                {
                    results.Add(stream);
                }
            }
            else if (buffer.EndsWith("trailer "))
            {
                ClearQueues();

                // Grab the last trailer dictionary as backup in case we find no valid xrefs.
                if (scanner.TryReadToken(out DictionaryToken trailerDict))
                {
                    trailer = trailerDict;
                }
            }
        }

        return new Result(
            results,
            bruteForceObjPositions,
            trailer);
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
