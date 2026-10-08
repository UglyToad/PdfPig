using System;
using System.IO;
using System.Runtime.CompilerServices;
using UglyToad.PdfPig.Fonts;

namespace UglyToad.PdfPig.Filters.CcittFax;
// Signed transition positions support widths above 65,535 and malformed positions accepted in
// lenient mode. Tables, reservoir and direct-polarity painting are shared with compact decoding.
// The signed-position implementation is original C# under the repository Apache-2.0 license.
// Compatibility rules retain behavior from PdfPig's former Apache-2.0 stream decoder, whose source
// attribution identifies this PDFBox implementation:
// https://github.com/apache/pdfbox/blob/e644c29279e276bde14ce7a33bdeef0cb1001b3e/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxDecoderStream.java
// Retained rules include transition ordering, pass-at-reference-end handling, EOL/EOFB consumption,
// strict/lenient failures, and discarding an incomplete row at EOF while preserving completed rows.
// The row representation and lookup optimizations are new; this is not an unchanged PDFBox port.
internal static partial class CcittFaxCompactDecoder
{
    // Signed/full-width positions handle wide images and compatibility cases.
    // Lookup tables, reservoir, run pairs and polarity painter are shared with the compact path.
    private sealed class FullWidthRows
    {
        private readonly int columns;
        private readonly CcittFaxCompressionType type;
        private readonly bool optionByteAligned, useLenientParsing;
        // A lenient row may contain signed, backwards or out-of-range transitions. Defer its
        // rendering until all transitions are known instead of clipping them and changing old output.
        private bool malformedRow;
        private int[] changesReferenceRow, changesCurrentRow;
        private int changesReferenceRowCount, changesCurrentRowCount, lastChangingElement;
        internal FullWidthRows(int columns, CcittFaxCompressionType type, bool aligned, bool lenient)
        {
            this.columns = columns;
            this.type = type;
            optionByteAligned = aligned;
            useLenientParsing = lenient;
            changesReferenceRow = new int[columns + 2];
            changesCurrentRow = new int[columns + 2];
        }

        internal void DecodeRow<T>(ref CcittFaxCompactBitReader bits, Span<byte> row)
            where T : struct, ICcittCompactFeatures
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
                        if (bits.ReadExact(1) == 0)
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

                    oneD = type == CcittFaxCompressionType.Group3_1D || bits.ReadExact(1) != 0;
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
            row.Fill(BlackOne<T>() ? (byte)0 : (byte)255);
            int x = 0;
            bool white = true;
            do
            {
                int operation = oneD ? 100 : Mode(ref bits);
                if (operation == int.MinValue)
                {
                    if (useLenientParsing)
                        continue;
                    if (type == CcittFaxCompressionType.Group4_2D && x == 0 && changesCurrentRowCount == 0 && bits.ReadExact(6) == 1 && bits.ReadExact(12) == 1)
                        throw new EndOfStreamException("CCITT end-of-facsimile block.");
                    throw new CorruptCompressedDataException("Unknown code in CCITT 2D stream.");
                }

                if (operation == 100)
                {
                    if (!malformedRow && bits.Ensure(12))
                    {
                        uint pair = (white ? Pairs16.WhitePairs : Pairs16.BlackPairs)[bits.PeekExact(12)];
                        int first = (int)(pair & 63), second = (int)((pair >> 6) & 63);
                        if (pair != 0 && first + second <= columns - x && (!oneD || first < columns - x))
                        {
                            bits.Drop((int)(pair >> 12));
                            int middle = x + first, end = middle + second;
                            Draw<T>(row, x, middle, white);
                            changesCurrentRow[changesCurrentRowCount++] = middle;
                            Draw<T>(row, middle, end, !white);
                            changesCurrentRow[changesCurrentRowCount++] = end;
                            x = end;
                            continue;
                        }
                    }

                    int next = AddRun(x, Run(ref bits, white));
                    Draw<T>(row, x, next, white);
                    changesCurrentRow[changesCurrentRowCount++] = next;
                    x = next;
                    if (oneD)
                        white = !white;
                    else
                    {
                        next = AddRun(x, Run(ref bits, !white));
                        Draw<T>(row, x, next, !white);
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
                    Draw<T>(row, x, next, white);
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
                row.Fill(BlackOne<T>() ? (byte)0 : (byte)255);
                RenderLegacy(row, BlackOne<T>());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Draw<T>(Span<byte> row, int start, int end, bool white)
            where T : struct, ICcittCompactFeatures
        {
            if (!malformedRow && !white && end > start)
                Paint<T>(row, start, end);
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
        private int Run(ref CcittFaxCompactBitReader bits, bool white)
        {
            var table = white ? White : Black;
            int lookahead = white ? 12 : 13, total = 0, value;
            do
            {
                int entry = bits.Ensure(lookahead) ? table[bits.PeekExact(lookahead)] : 0;
                if (entry != 0)
                {
                    bits.Drop(entry & 15);
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

        private static int ReadRunTail(ref CcittFaxCompactBitReader bits, CcittCode[] codes)
        {
            int prefix = 0;
            for (int length = 1; length <= 13; length++)
            {
                prefix = (prefix << 1) | bits.ReadExact(1);
                if (length == 12 && prefix == 1)
                    // Historical EOL marker, not a negative run. Run translates it to the row width.
                    return -2000;
                if (length == 12 && prefix == 0)
                {
                    while (bits.ReadExact(1) == 0)
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
        private static int Mode(ref CcittFaxCompactBitReader bits)
        {
            if (bits.Ensure(7))
            {
                int entry = Modes[bits.PeekExact(7)];
                if (entry != 0)
                {
                    bits.Drop(entry & 7);
                    return entry >> 3;
                }
            }

            // Preserve six-bit invalid-prefix consumption and strict EOFB detection.
            if (bits.ReadExact(1) != 0)
                return 0;
            int pair = bits.ReadExact(2);
            if (pair == 1)
                return 100;
            if (pair >= 2)
                return pair == 3 ? 1 : -1;
            if (bits.ReadExact(1) != 0)
                return 101;
            int tail = bits.ReadExact(2);
            if (tail >= 2)
                return tail == 3 ? 2 : -2;
            if (tail == 1)
                return bits.ReadExact(1) != 0 ? 3 : -3;
            return int.MinValue;
        }

        // Compatibility rendering for malformed changing positions accepted in lenient mode.
        // Keep the former transition order and pixel-write rules: sorting, clipping or merging
        // these positions would change bytes returned for PDFs accepted by the previous decoder.
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

    internal static void Decode(ReadOnlySpan<byte> input, byte[] output, int width, int rows, CcittFaxCompressionType mode, bool aligned, bool blackIsOne, bool lenient)
    {
        if (TryDecode(input, output, width, rows, mode, aligned, blackIsOne))
            return;
        // Restart from the original input: a failed compact probe may have consumed part of a row.
        // Restarting only on failure avoids saving reader and row checkpoints on every successful row.
        DecodeFullWidth(input, output, width, rows, mode, aligned, blackIsOne, lenient);
    }

    // Uses shared tables, bit reader, run pairs and painter; no separate legacy decoder.
    internal static void DecodeFullWidth(ReadOnlySpan<byte> input, byte[] output, int width, int rows, CcittFaxCompressionType mode, bool aligned, bool blackIsOne, bool lenient)
    {
        var bits = new CcittFaxCompactBitReader(input);
        if (blackIsOne)
            DecodeRemainingRows<Features6OneGeneral>(ref bits, output, width, rows, mode, aligned, lenient);
        else
            DecodeRemainingRows<Features6ZeroGeneral>(ref bits, output, width, rows, mode, aligned, lenient);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool BlackOne<T>()
        where T : struct, ICcittCompactFeatures
    {
#if NET8_0_OR_GREATER
        return T.BlackOne;
#else
        return default(T).BlackOne;
#endif
    }

    private static void DecodeRemainingRows<T>(ref CcittFaxCompactBitReader bits, byte[] output, int width, int rows, CcittFaxCompressionType mode, bool aligned, bool lenient)
        where T : struct, ICcittCompactFeatures
    {
        int stride = (width + 7) / 8;
        var state = new FullWidthRows(width, mode, aligned, lenient);
        for (int y = 0; y < rows; y++)
        {
            try
            {
                state.DecodeRow<T>(ref bits, output.AsSpan(y * stride, stride));
            }
            catch (IndexOutOfRangeException e)
            {
                throw new CorruptCompressedDataException("Malformed CCITT stream: decoder buffer bounds exceeded.", e);
            }
            catch (OverflowException e)
            {
                throw new CorruptCompressedDataException("Malformed CCITT stream: run arithmetic overflow.", e);
            }
            // Preserve completed rows, discard the incomplete row and fill the remainder white.
            // The fill includes padding and uses the requested polarity in both recovery cases.
            catch (EndOfStreamException)
            {
                output.AsSpan(y * stride).Fill(BlackOne<T>() ? (byte)0 : (byte)255);
                return;
            }
            catch (CorruptCompressedDataException) when (lenient)
            {
                output.AsSpan(y * stride).Fill(BlackOne<T>() ? (byte)0 : (byte)255);
                return;
            }
        }
    }
}
