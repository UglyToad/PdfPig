namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Undoes the predictor a producer applied before compressing a stream, for the Flate, LZW and
    /// Brotli filters. See ISO 32000-1 section 7.4.4.4: the TIFF predictor stores each sample as the
    /// difference to the sample on its left, the PNG predictors store each byte as the difference to
    /// a neighbour chosen per row, with the row's filter type in a byte ahead of the row.
    /// </summary>
    /// <remarks>
    /// Rows are decoded where they lie, with the row before them - already decoded, directly ahead
    /// in the same buffer - as the reference, so nothing is copied into and out of row buffers and no
    /// second stream is written. The PNG filter type bytes are squeezed out as the rows move up. The
    /// <see cref="Decoder"/> does this a piece at a time, so a filter can decode rows as soon as it
    /// has inflated them, while they are still in the cache; <see cref="Decode"/> does it for data
    /// that is already complete.
    /// </remarks>
    internal static class PngPredictor
    {
        private const int TiffPredictor = 2;

        /// <summary>Predictors from this value on are the PNG filters, chosen per row.</summary>
        private const int FirstPngPredictor = 10;

        private const byte PngNone = 0;
        private const byte PngSub = 1;
        private const byte PngUp = 2;
        private const byte PngAverage = 3;
        private const byte PngPaeth = 4;

        /// <summary>
        /// Undoes the predictor over complete decoded data. For predictor values below 2 the data is
        /// returned as it is.
        /// </summary>
        /// <param name="data">The decompressed data. It is decoded in place, so it must not be needed afterwards.</param>
        /// <param name="predictor">The Predictor decode parameter.</param>
        /// <param name="colors">The Colors decode parameter.</param>
        /// <param name="bitsPerComponent">The BitsPerComponent decode parameter.</param>
        /// <param name="columns">The Columns decode parameter.</param>
        /// <returns>
        /// The decoded rows: a slice of <paramref name="data"/>, or a new array where an incomplete last
        /// row had to be padded to its full length and that did not fit.
        /// </returns>
        public static Memory<byte> Decode(Memory<byte> data, int predictor, int colors, int bitsPerComponent, int columns)
        {
            if (predictor <= 1 || data.Length == 0)
            {
                return data;
            }

            var decoder = new Decoder(predictor, colors, bitsPerComponent, columns);
            var span = data.Span;

            decoder.Advance(span, span.Length);

            var finalLength = decoder.FinalLength(span.Length);

            if (finalLength <= span.Length)
            {
                decoder.Finish(span, span.Length);

                return data.Slice(0, finalLength);
            }

            // Only a padded last row makes the output longer than the input, and then there are
            // fewer rows than bytes in a row, so the undecoded tail sits below the final length and
            // can stay at its index in the longer buffer.
            var grown = new byte[finalLength];

            span.Slice(0, decoder.DecodedLength).CopyTo(grown);
            span.Slice(decoder.ConsumedLength).CopyTo(grown.AsSpan(decoder.ConsumedLength));

            decoder.Finish(grown, span.Length);

            return grown;
        }

        /// <summary>
        /// The number of bytes in a row of the decoded data.
        /// </summary>
        public static int CalculateRowLength(int colors, int bitsPerComponent, int columns)
        {
            var bitsPerPixel = colors * bitsPerComponent;
            return ((columns * bitsPerPixel) + 7) / 8;
        }

        /// <summary>
        /// Undoes a predictor over data that arrives in pieces, in the buffer the pieces arrive in.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Advance"/> whenever more data has been appended to the buffer: every row
        /// that is complete by then is decoded and moved up to follow the rows before it. Input that
        /// does not yet make up a row stays where it is, at <see cref="ConsumedLength"/> and beyond,
        /// so further data is appended after it as usual. <see cref="Finish"/> deals with a last row
        /// that was cut short, once the input has ended.
        /// </remarks>
        public struct Decoder
        {
            private readonly int predictor;
            private readonly int colors;
            private readonly int bitsPerComponent;
            private readonly int columns;
            private readonly int rowLength;
            private readonly int bytesPerPixel;

            /// <summary>The bytes a row occupies in the input: the row, plus its filter type for the PNG predictors.</summary>
            private readonly int stride;

            private byte[]? zeroRow;

            /// <summary>
            /// Creates a decoder for the given decode parameters. The predictor must be 2 or more.
            /// </summary>
            public Decoder(int predictor, int colors, int bitsPerComponent, int columns)
            {
                this.predictor = predictor;
                this.colors = colors;
                this.bitsPerComponent = bitsPerComponent;
                this.columns = columns;

                rowLength = CalculateRowLength(colors, bitsPerComponent, columns);
                bytesPerPixel = ((colors * bitsPerComponent) + 7) / 8;
                stride = predictor >= FirstPngPredictor ? rowLength + 1 : rowLength;

                zeroRow = null;
                DecodedLength = 0;
                ConsumedLength = 0;
            }

            /// <summary>How many bytes of decoded rows the buffer holds, from its start.</summary>
            public int DecodedLength { get; private set; }

            /// <summary>How many bytes of input have been decoded; input from here on is still waiting for its row to complete.</summary>
            public int ConsumedLength { get; private set; }

            /// <summary>
            /// Decodes every row that is complete within the first <paramref name="available"/> bytes of
            /// <paramref name="buffer"/> and has not been decoded yet.
            /// </summary>
            public void Advance(Span<byte> buffer, int available)
            {
                while (available - ConsumedLength >= stride)
                {
                    DecodeNextRow(buffer, rowLength);
                }
            }

            /// <summary>
            /// The length the decoded data has once the input ends after <paramref name="available"/>
            /// bytes: the complete rows, plus a padded row if input for part of one is left over.
            /// </summary>
            public int FinalLength(int available)
            {
                return DecodedLength + (PartialRowLength(available) > 0 ? rowLength : 0);
            }

            /// <summary>
            /// Decodes a last row that was cut short, padding it with zeros, and returns the length of the
            /// decoded data. The buffer must hold at least <see cref="FinalLength"/> bytes.
            /// </summary>
            public int Finish(Span<byte> buffer, int available)
            {
                var partial = PartialRowLength(available);

                if (partial > 0)
                {
                    DecodeNextRow(buffer, partial);
                }

                return DecodedLength;
            }

            /// <summary>
            /// How many bytes of row data are left over after the complete rows. A trailing filter type
            /// byte with nothing after it is not a row.
            /// </summary>
            private int PartialRowLength(int available)
            {
                var tail = available - ConsumedLength;

                return Math.Max(0, stride == rowLength ? tail : tail - 1);
            }

            /// <summary>
            /// Moves the next row up to <see cref="DecodedLength"/>, pads it to the row length if it is
            /// short, and decodes it against the row before.
            /// </summary>
            private void DecodeNextRow(Span<byte> buffer, int dataLength)
            {
                // The row is written to [DecodedLength, DecodedLength + rowLength), which ends at or
                // before where its own input starts, so the input is intact until it is moved; the
                // move overlaps with the destination ahead of the source, which CopyTo handles.
                var isPng = stride != rowLength;
                var filterType = isPng ? buffer[ConsumedLength] : (byte)0;
                var source = buffer.Slice(ConsumedLength + (isPng ? 1 : 0), dataLength);
                var row = buffer.Slice(DecodedLength, rowLength);

                if (isPng || dataLength < rowLength)
                {
                    source.CopyTo(row);
                }

                if (dataLength < rowLength)
                {
                    row.Slice(dataLength).Clear();
                }

                ReadOnlySpan<byte> previous;
                if (DecodedLength == 0)
                {
                    zeroRow ??= new byte[rowLength];
                    previous = zeroRow;
                }
                else
                {
                    previous = buffer.Slice(DecodedLength - rowLength, rowLength);
                }

                if (isPng)
                {
                    DecodePngRow(filterType, row, previous, bytesPerPixel);
                }
                else if (predictor == TiffPredictor)
                {
                    DecodeTiffRow(row, colors, bitsPerComponent, columns);
                }

                // Values 3 to 9 are not defined and leave the rows as they are.

                ConsumedLength += (isPng ? 1 : 0) + dataLength;
                DecodedLength += rowLength;
            }
        }

        private static void DecodePngRow(byte filterType, Span<byte> row, ReadOnlySpan<byte> previous, int bytesPerPixel)
        {
            switch (filterType)
            {
                case PngNone:
                    break;
                case PngSub:
                    Sub(row, bytesPerPixel);
                    break;
                case PngUp:
                    Up(row, previous);
                    break;
                case PngAverage:
                    Average(row, previous, bytesPerPixel);
                    break;
                case PngPaeth:
                    Paeth(row, previous, bytesPerPixel);
                    break;
                default:
                    // Not a filter type the PNG specification knows; the row is left as it is.
                    break;
            }
        }

        /// <summary>Each byte was stored as the difference to the byte one pixel to its left.</summary>
        private static void Sub(Span<byte> row, int bytesPerPixel)
        {
            // Every byte depends on the byte one pixel back, which the plain loop has only just
            // stored: reading it back costs the store-to-load latency per byte, and that, not the
            // arithmetic, is what bounds the loop. For the usual pixel widths the left pixel is
            // carried in locals instead, so the chain is a register add.
            var i = bytesPerPixel;

            switch (bytesPerPixel)
            {
                case 1 when row.Length >= 1:
                {
                    var a = row[0];

                    for (; i < row.Length; i++)
                    {
                        a = row[i] += a;
                    }

                    break;
                }

                case 3 when row.Length >= 3:
                {
                    var a = row[0];
                    var b = row[1];
                    var c = row[2];

                    for (; i + 2 < row.Length; i += 3)
                    {
                        a = row[i] += a;
                        b = row[i + 1] += b;
                        c = row[i + 2] += c;
                    }

                    break;
                }

                case 4 when row.Length >= 4:
                {
                    var a = row[0];
                    var b = row[1];
                    var c = row[2];
                    var d = row[3];

                    for (; i + 3 < row.Length; i += 4)
                    {
                        a = row[i] += a;
                        b = row[i + 1] += b;
                        c = row[i + 2] += c;
                        d = row[i + 3] += d;
                    }

                    break;
                }
            }

            // Other widths, and whatever is left when the row is not a whole number of pixels.
            for (; i < row.Length; i++)
            {
                row[i] += row[i - bytesPerPixel];
            }
        }

        /// <summary>Each byte was stored as the difference to the byte above it.</summary>
        private static void Up(Span<byte> row, ReadOnlySpan<byte> previous)
        {
            // No byte depends on another in this row, so whole vectors of them are added at once.
            var i = 0;

            if (Vector.IsHardwareAccelerated && row.Length >= Vector<byte>.Count)
            {
                var rowVectors = MemoryMarshal.Cast<byte, Vector<byte>>(row);
                var previousVectors = MemoryMarshal.Cast<byte, Vector<byte>>(previous.Slice(0, row.Length));

                for (var v = 0; v < rowVectors.Length; v++)
                {
                    rowVectors[v] += previousVectors[v];
                }

                i = rowVectors.Length * Vector<byte>.Count;
            }

            for (; i < row.Length; i++)
            {
                row[i] += previous[i];
            }
        }

        /// <summary>Each byte was stored as the difference to the mean of the bytes to its left and above it.</summary>
        private static void Average(Span<byte> row, ReadOnlySpan<byte> previous, int bytesPerPixel)
        {
            // In the first pixel there is nothing to the left, so the mean is half the byte above.
            var head = Math.Min(bytesPerPixel, row.Length);

            for (var h = 0; h < head; h++)
            {
                row[h] += (byte)(previous[h] >> 1);
            }

            // The left neighbour is carried in locals for the usual pixel widths, as in Sub.
            var i = head;

            switch (bytesPerPixel)
            {
                case 1 when row.Length >= 1:
                {
                    var a = row[0];

                    for (; i < row.Length; i++)
                    {
                        a = row[i] += (byte)((a + previous[i]) >> 1);
                    }

                    break;
                }

                case 3 when row.Length >= 3:
                {
                    var a = row[0];
                    var b = row[1];
                    var c = row[2];

                    for (; i + 2 < row.Length; i += 3)
                    {
                        a = row[i] += (byte)((a + previous[i]) >> 1);
                        b = row[i + 1] += (byte)((b + previous[i + 1]) >> 1);
                        c = row[i + 2] += (byte)((c + previous[i + 2]) >> 1);
                    }

                    break;
                }

                case 4 when row.Length >= 4:
                {
                    var a = row[0];
                    var b = row[1];
                    var c = row[2];
                    var d = row[3];

                    for (; i + 3 < row.Length; i += 4)
                    {
                        a = row[i] += (byte)((a + previous[i]) >> 1);
                        b = row[i + 1] += (byte)((b + previous[i + 1]) >> 1);
                        c = row[i + 2] += (byte)((c + previous[i + 2]) >> 1);
                        d = row[i + 3] += (byte)((d + previous[i + 3]) >> 1);
                    }

                    break;
                }
            }

            for (; i < row.Length; i++)
            {
                row[i] += (byte)((row[i - bytesPerPixel] + previous[i]) >> 1);
            }
        }

        /// <summary>
        /// Each byte was stored as the difference to whichever of the bytes to its left (a), above it (b)
        /// and above-left (c) lies closest to a + b - c.
        /// </summary>
        private static void Paeth(Span<byte> row, ReadOnlySpan<byte> previous, int bytesPerPixel)
        {
            // In the first pixel a and c are zero, and the estimate then always picks b.
            var head = Math.Min(bytesPerPixel, row.Length);

            for (var h = 0; h < head; h++)
            {
                row[h] += previous[h];
            }

            // Carrying the left neighbour in locals, as Sub and Average do, was measured and gains
            // nothing here: the prediction itself is the long dependency, not the reload.
            for (var i = head; i < row.Length; i++)
            {
                row[i] += Predict(row[i - bytesPerPixel], previous[i], previous[i - bytesPerPixel]);
            }
        }

        /// <summary>
        /// The Paeth predictor: whichever of a (left), b (above) and c (above-left) lies closest to
        /// a + b - c, a first, then b, then c on ties, as the PNG specification orders it.
        /// </summary>
        /// <remarks>
        /// Without branches. Image data makes the choice unpredictable, and a mispredicted branch
        /// per byte costs more than the whole calculation. The distances are compared through sign
        /// bits and the choice made with masks: notA is all ones when a loses to b or c, chooseC
        /// when c beats b.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Predict(int a, int b, int c)
        {
            var pa = b - c;
            var pb = a - c;
            var pc = pa + pb;

            pa = (pa ^ (pa >> 31)) - (pa >> 31);
            pb = (pb ^ (pb >> 31)) - (pb >> 31);
            pc = (pc ^ (pc >> 31)) - (pc >> 31);

            var notA = ((pb - pa) | (pc - pa)) >> 31;
            var chooseC = (pc - pb) >> 31;
            var bOrC = (b & ~chooseC) | (c & chooseC);

            return (byte)((a & ~notA) | (bOrC & notA));
        }

        /// <summary>
        /// Each sample was stored as the difference to the sample one pixel to its left, at whatever
        /// width a sample has.
        /// </summary>
        private static void DecodeTiffRow(Span<byte> row, int colors, int bitsPerComponent, int columns)
        {
            var bytesPerPixel = ((colors * bitsPerComponent) + 7) / 8;

            if (bitsPerComponent == 8)
            {
                // At a byte per sample this is the PNG Sub filter.
                Sub(row, bytesPerPixel);
            }
            else if (bitsPerComponent == 16)
            {
                for (var i = bytesPerPixel; i < row.Length - 1; i += 2)
                {
                    var value = (row[i] << 8) + row[i + 1];
                    var left = (row[i - bytesPerPixel] << 8) + row[i - bytesPerPixel + 1];
                    var sum = value + left;

                    row[i] = (byte)(sum >> 8);
                    row[i + 1] = (byte)sum;
                }
            }
            else if (bitsPerComponent == 1 && colors == 1)
            {
                // A row is a whole number of bytes with the samples packed from the high bit down,
                // so a pixel is a bit and the pixel to its left is the bit above it, or the low bit
                // of the byte before.
                for (var i = 0; i < row.Length; i++)
                {
                    for (var bit = 7; bit >= 0; bit--)
                    {
                        if (i == 0 && bit == 7)
                        {
                            continue;
                        }

                        var value = (row[i] >> bit) & 1;
                        var left = bit == 7 ? row[i - 1] & 1 : (row[i] >> (bit + 1)) & 1;

                        if (((value + left) & 1) == 0)
                        {
                            row[i] &= (byte)~(1 << bit);
                        }
                        else
                        {
                            row[i] |= (byte)(1 << bit);
                        }
                    }
                }
            }
            else
            {
                // Samples of 2 or 4 bits, and any other width, read and written bit field by bit field.
                var samples = columns * colors;
                var mask = (1 << bitsPerComponent) - 1;

                for (var sample = colors; sample < samples; sample++)
                {
                    var bytePosition = sample * bitsPerComponent / 8;
                    var bitPosition = 8 - (sample * bitsPerComponent % 8) - bitsPerComponent;
                    var leftBytePosition = (sample - colors) * bitsPerComponent / 8;
                    var leftBitPosition = 8 - ((sample - colors) * bitsPerComponent % 8) - bitsPerComponent;

                    var value = (row[bytePosition] >> bitPosition) & mask;
                    var left = (row[leftBytePosition] >> leftBitPosition) & mask;
                    var sum = (value + left) & mask;

                    row[bytePosition] = (byte)((row[bytePosition] & ~(mask << bitPosition)) | (sum << bitPosition));
                }
            }
        }
    }
}
