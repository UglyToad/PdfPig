// Frozen pre-consolidation test oracle. Never used by production decoding.
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UglyToad.PdfPig.Fonts;

namespace UglyToad.PdfPig.Tests.Filters.CcittBaseline;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Filters.CcittFax;
/// <summary>
/// Flat T.4/T.6 transition decoder. Compact run and mode tables follow the LibTIFF
/// lookup approach; code definitions and lenient semantics are retained from PdfPig/PDFBox.
/// </summary>
internal sealed class CcittFaxDecoder
{
    private readonly int columns;
    private readonly CcittFaxCompressionType type;
    private readonly bool optionByteAligned, useLenientParsing;
    private readonly ReadOnlyMemory<byte> input;
    private int offset, count;
    private ulong reservoir;
    private bool ended, malformedRow;
    private int[] changesReferenceRow, changesCurrentRow;
    private int changesReferenceRowCount, changesCurrentRowCount, lastChangingElement;
    private static readonly ushort[] White = Build(CcittFaxCodebook.White, 12), Black = Build(CcittFaxCodebook.Black, 13);
    private static readonly int[] Modes = BuildModes();
    private CcittFaxDecoder(int columns, CcittFaxCompressionType type, bool aligned, bool lenient)
    {
        this.columns = columns;
        this.type = type;
        optionByteAligned = aligned;
        useLenientParsing = lenient;
        changesReferenceRow = new int[columns + 2];
        changesCurrentRow = new int[columns + 2];
    }

    internal CcittFaxDecoder(ReadOnlyMemory<byte> input, int columns, CcittFaxCompressionType type, bool aligned, bool lenient) : this(columns, type, aligned, lenient)
    {
        this.input = input;
    }

    private static ushort[] Build(CcittCode[] codes, int bits)
    {
        var table = new ushort[1 << bits];
        foreach (var code in codes)
            table.AsSpan(code.Bits << (bits - code.Length), 1 << (bits - code.Length)).Fill(checked((ushort)((code.Run << 4) | code.Length)));
        return table;
    }

    private static int[] BuildModes()
    {
        var table = new int[128];
        foreach (var code in new[]
        {
            new CcittCode(1, 1, 0),
            new CcittCode(3, 3, 1),
            new CcittCode(2, 3, -1),
            new CcittCode(1, 3, 100),
            new CcittCode(1, 4, 101),
            new CcittCode(3, 6, 2),
            new CcittCode(2, 6, -2),
            new CcittCode(3, 7, 3),
            new CcittCode(2, 7, -3)
        }

        )
            table.AsSpan(code.Bits << (7 - code.Length), 1 << (7 - code.Length)).Fill((code.Run << 3) | code.Length);
        return table;
    }

    internal void DecodeInto(byte[] destination, bool blackIsOne)
    {
        int stride = (columns + 7) / 8;
        if (destination.Length % stride != 0)
            throw new ArgumentException("The bitmap must contain complete CCITT rows.", nameof(destination));
        var bits = new CcittFaxBitReader(input.Span, offset, count, reservoir);
        try
        {
            for (int y = 0; y < destination.Length; y += stride)
            {
                var row = destination.AsSpan(y, stride);
                if (!TryDecodeRow(ref bits, row))
                {
                    destination.AsSpan(y).Fill(blackIsOne ? (byte)0 : (byte)255);
                    return;
                }

                if (!blackIsOne)
                    CcittFaxDecodeFilter.InvertBitmap(row);
            }
        }
        finally
        {
            Save(ref bits);
        }
    }

    private void Save(ref CcittFaxBitReader bits)
    {
        offset = bits.Offset;
        count = bits.Count;
        reservoir = bits.Reservoir;
    }

    private bool TryDecodeRow(ref CcittFaxBitReader bits, Span<byte> row)
    {
        if (ended)
            return false;
        try
        {
            DecodeRow(ref bits, row);
            return true;
        }
        catch (IndexOutOfRangeException e)
        {
            throw new CorruptCompressedDataException("Malformed CCITT stream: decoder buffer bounds exceeded.", e);
        }
        catch (OverflowException e)
        {
            throw new CorruptCompressedDataException("Malformed CCITT stream: run arithmetic overflow.", e);
        }
        catch (EndOfStreamException)
        {
            ended = true;
            return false;
        }
        catch (CorruptCompressedDataException) when (useLenientParsing)
        {
            ended = true;
            return false;
        }
    }

    private void DecodeRow(ref CcittFaxBitReader bits, Span<byte> row)
    {
        if (optionByteAligned)
            bits.Align();
        bool oneD;
        switch (type)
        {
            case CcittFaxCompressionType.ModifiedHuffman:
                oneD = true;
                break;
            case CcittFaxCompressionType.Group4_2D:
                oneD = false;
                break;
            case CcittFaxCompressionType.Group3_1D:
            case CcittFaxCompressionType.Group3_2D:
                int zeros = 0;
                while (true)
                {
                    if (bits.Read(1) == 0)
                    {
                        // Only the EOL threshold matters, even for very long fill sequences.
                        if (zeros < 11)
                            zeros++;
                    }
                    else if (zeros >= 11)
                        break;
                    else
                        zeros = 0;
                }

                oneD = type == CcittFaxCompressionType.Group3_1D || bits.Read(1) != 0;
                break;
            default:
                throw new InvalidOperationException(type + " is not a supported compression type.");
        }

        if (!oneD)
        {
            changesReferenceRowCount = changesCurrentRowCount;
            var tmp = changesCurrentRow;
            changesCurrentRow = changesReferenceRow;
            changesReferenceRow = tmp;
        }

        changesCurrentRowCount = 0;
        malformedRow = false;
        row.Clear();
        int x = 0;
        bool white = true;
        do
        {
            int operation = oneD ? 100 : Mode(ref bits);
            if (operation == int.MinValue)
            {
                if (useLenientParsing)
                    continue;
                if (type == CcittFaxCompressionType.Group4_2D && x == 0 && changesCurrentRowCount == 0 && bits.Read(6) == 1 && bits.Read(12) == 1)
                    throw new EndOfStreamException("CCITT end-of-facsimile block.");
                throw new CorruptCompressedDataException("Unknown code in CCITT 2D stream.");
            }

            if (operation == 100)
            {
                int next = AddRun(x, Run(ref bits, white));
                Draw(row, x, next, white);
                changesCurrentRow[changesCurrentRowCount++] = next;
                x = next;
                if (oneD)
                    white = !white;
                else
                {
                    next = AddRun(x, Run(ref bits, !white));
                    Draw(row, x, next, !white);
                    changesCurrentRow[changesCurrentRowCount++] = next;
                    x = next;
                }
            }
            else
            {
                int b = GetNextChangingElement(x, white);
                int next;
                if (operation == 101)
                {
                    b++;
                    next = b >= changesReferenceRowCount ? columns : changesReferenceRow[b];
                }
                else
                {
                    next = checked((b >= changesReferenceRowCount || b == -1 ? columns : changesReferenceRow[b]) + operation);
                }

                ValidatePosition(x, next);
                Draw(row, x, next, white);
                x = next;
                if (operation != 101)
                {
                    changesCurrentRow[changesCurrentRowCount++] = x;
                    white = !white;
                }
            }
        }
        while (x < columns);
        lastChangingElement = 0;
        if (malformedRow)
        {
            row.Clear();
            RenderLegacy(row, true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Draw(Span<byte> row, int start, int end, bool white)
    {
        if (malformedRow || white || end <= start)
            return;
        int first = start >> 3, last = (end - 1) >> 3;
        if (first == last)
        {
            row[first] |= (byte)((255 >> (start & 7)) & (255 << (8 - ((end - 1 & 7) + 1))));
            return;
        }

        row[first] |= (byte)(255 >> (start & 7));
        row.Slice(first + 1, last - first - 1).Fill(255);
        row[last] |= (byte)(255 << (7 - ((end - 1) & 7)));
    }

    private int AddRun(int position, int run)
    {
        int next = checked(position + run);
        ValidatePosition(position, next);
        return next;
    }

    private void ValidatePosition(int previous, int next)
    {
        if (next < previous || next < 0 || next > columns)
        {
            if (!useLenientParsing)
                throw new CorruptCompressedDataException($"Invalid CCITT changing position: {next}, previous={previous}, columns={columns}.");
            malformedRow = true;
        }
    }

    private int GetNextChangingElement(int a0, bool white)
    {
        int start = (lastChangingElement & ~1) + (white ? 0 : 1);
        if (start > 2)
            start -= 2;
        if (a0 == 0)
            return start;
        for (int i = start; i < changesReferenceRowCount; i += 2)
            if (a0 < changesReferenceRow[i])
            {
                lastChangingElement = i;
                return i;
            }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Run(ref CcittFaxBitReader bits, bool white)
    {
        var table = white ? White : Black;
        int lookahead = white ? 12 : 13, total = 0, value;
        do
        {
            int entry = bits.Ensure(lookahead) ? table[bits.Peek(lookahead)] : 0;
            if (entry != 0)
            {
                bits.Count -= entry & 15;
                value = entry >> 4;
            }
            else
                value = ReadRunTail(ref bits, white ? CcittFaxCodebook.White : CcittFaxCodebook.Black);
            total = checked(total + value);
            if (value >= 0)
                ValidatePosition(0, total);
        }
        while (value >= 64);
        return value >= 0 ? total : columns;
    }

    private static int ReadRunTail(ref CcittFaxBitReader bits, CcittCode[] codes)
    {
        int prefix = 0;
        for (int length = 1; length <= 13; length++)
        {
            prefix = (prefix << 1) | bits.Read(1);
            if (length == 12 && prefix == 1)
                return -2000;
            if (length == 12 && prefix == 0)
            {
                while (bits.Read(1) == 0)
                {
                }

                return -2000;
            }

            bool possible = prefix == 0 && length < 12;
            foreach (var code in codes)
            {
                if (code.Length < length || (code.Bits >> (code.Length - length)) != prefix)
                    continue;
                if (code.Length == length)
                    return code.Run;
                possible = true;
            }

            if (!possible)
                throw new CorruptCompressedDataException("Unknown code in Huffman RLE stream");
        }

        throw new CorruptCompressedDataException("Unknown code in Huffman RLE stream");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Mode(ref CcittFaxBitReader bits)
    {
        if (bits.Ensure(7))
        {
            int entry = Modes[bits.Peek(7)];
            if (entry != 0)
            {
                bits.Count -= entry & 7;
                return entry >> 3;
            }
        }

        // Preserve six-bit invalid-prefix consumption and strict EOFB detection.
        if (bits.Read(1) != 0)
            return 0;
        int pair = bits.Read(2);
        if (pair == 1)
            return 100;
        if (pair >= 2)
            return pair == 3 ? 1 : -1;
        if (bits.Read(1) != 0)
            return 101;
        int tail = bits.Read(2);
        if (tail >= 2)
            return tail == 3 ? 2 : -2;
        if (tail == 1)
            return bits.Read(1) != 0 ? 3 : -3;
        return int.MinValue;
    }

    // Compatibility rendering for malformed changing positions accepted in lenient mode.
    private void RenderLegacy(Span<byte> destination, bool blackIsOne)
    {
        var index = 0;
        var white = true;
        lastChangingElement = 0;
        for (var i = 0; i <= changesCurrentRowCount; i++)
        {
            var nextChange = columns;
            if (i != changesCurrentRowCount)
            {
                nextChange = changesCurrentRow[i];
            }

            ValidatePosition(index, nextChange);
            if (nextChange > columns)
            {
                nextChange = columns;
            }

            if (index >= 0 && nextChange >= index)
            {
                var byteIndex = index / 8;
                var bitOffset = index & 7;
                if (bitOffset != 0)
                {
                    var count = Math.Min(8 - bitOffset, nextChange - index);
                    var mask = (byte)((255 >> bitOffset) & (255 << (8 - bitOffset - count)));
                    if (blackIsOne)
                        destination[byteIndex] |= (byte)(white ? 0 : mask);
                    else
                        destination[byteIndex] &= (byte)~(white ? 0 : mask);
                    index += count;
                    if ((index & 7) == 0)
                        byteIndex++;
                }

                var byteCount = (nextChange - index) / 8;
                if (byteCount != 0)
                {
                    destination.Slice(byteIndex, byteCount).Fill((byte)(white == blackIsOne ? 0 : 255));
                    index += byteCount * 8;
                    byteIndex += byteCount;
                }

                if (nextChange > index)
                {
                    var mask = (byte)(255 << (8 - (nextChange - index)));
                    destination[byteIndex] = blackIsOne ? (byte)(white ? 0 : mask) : (byte)~(white ? 0 : mask);
                    index = nextChange;
                }
            }
            else
            {
                // Preserve legacy rendering for malformed positions accepted in lenient mode.
                var byteIndex = index / 8;
                while (index % 8 != 0 && nextChange - index > 0)
                {
                    var mask = (byte)(white ? 0 : 1 << 7 - index % 8);
                    if (blackIsOne)
                        destination[byteIndex] |= mask;
                    else
                        destination[byteIndex] &= (byte)~mask;
                    index++;
                }

                if (index % 8 == 0)
                {
                    byteIndex = index / 8;
                    var value = (byte)(white == blackIsOne ? 0x00 : 0xff);
                    if (nextChange - index > 7)
                    {
                        var byteCount = (nextChange - index) / 8;
                        destination.Slice(byteIndex, byteCount).Fill(value);
                        index += byteCount * 8;
                        byteIndex += byteCount;
                    }
                }

                while (nextChange - index > 0)
                {
                    if (index % 8 == 0)
                    {
                        destination[byteIndex] = blackIsOne ? (byte)0 : (byte)255;
                    }

                    var mask = (byte)(white ? 0 : 1 << 7 - index % 8);
                    if (blackIsOne)
                        destination[byteIndex] |= mask;
                    else
                        destination[byteIndex] &= (byte)~mask;
                    index++;
                }
            }

            white = !white;
        }

        if (index != columns)
        {
            throw new CorruptCompressedDataException($"Sum of run-lengths does not equal scan line width: {index} > {columns}");
        }
    }
}
