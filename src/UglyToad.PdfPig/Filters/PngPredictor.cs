namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.IO;

    // https://github.com/apache/pdfbox/blob/a53a70db16ea3133994120bcf1e216b9e760c05b/pdfbox/src/main/java/org/apache/pdfbox/filter/Predictor.java#L30

    /// <summary>
    /// Helper class to contain predictor decoding used by Flate and LZW filter. 
    /// </summary>
    internal static class PngPredictor
    {
        /// <summary>
        /// Decodes a single line of data in-place.
        /// </summary>
        /// <param name="predictor">Predictor value for the current line.</param>
        /// <param name="colors">Number of color components, from decode parameters.</param>
        /// <param name="bitsPerComponent">Number of bits per components, from decode parameters.</param>
        /// <param name="columns">Number samples in a row, from decode parameters.</param>
        /// <param name="actline">Current (active) line to decode. Data will be decoded in-place, i.e. - the contents of this buffer will be modified.</param>
        /// <param name="lastline">The previous decoded line. When decoding the first line, this parameter should be an empty byte array of the same length as <c>actline</c>.</param>
        public static void DecodePredictorRow(int predictor, int colors, int bitsPerComponent, int columns, byte[] actline, byte[] lastline)
        {
            if (predictor == 1)
            {
                // no prediction
                return;
            }

            int bitsPerPixel = colors * bitsPerComponent;
            int bytesPerPixel = (bitsPerPixel + 7) / 8;
            int rowLength = actline.Length;

            switch (predictor)
            {
                case 2:
                    // PRED TIFF SUB
                    if (bitsPerComponent == 8)
                    {
                        // for 8 bits per component it is the same algorithm as PRED SUB of PNG format
                        for (int p = bytesPerPixel; p < rowLength; p++)
                        {
                            int sub = actline[p] & 0xff;
                            int left = actline[p - bytesPerPixel] & 0xff;
                            actline[p] = (byte)(sub + left);
                        }
                    }
                    else if (bitsPerComponent == 16)
                    {
                        for (int p = bytesPerPixel; p < rowLength - 1; p += 2)
                        {
                            int sub = ((actline[p] & 0xff) << 8) + (actline[p + 1] & 0xff);
                            int left = ((actline[p - bytesPerPixel] & 0xff) << 8) + (actline[p - bytesPerPixel + 1] & 0xff);
                            int sum = sub + left;
                            actline[p] = (byte)((sum >> 8) & 0xff);
                            actline[p + 1] = (byte)(sum & 0xff);
                        }
                    }
                    else if (bitsPerComponent == 1 && colors == 1)
                    {
                        // bytesPerPixel cannot be used:
                        // "A row shall occupy a whole number of bytes, rounded up if necessary.
                        // Samples and their components shall be packed into bytes
                        // from high-order to low-order bits."

                        for (int p = 0; p < rowLength; p++)
                        {
                            for (int bit = 7; bit >= 0; --bit)
                            {
                                int sub = (actline[p] >> bit) & 1;
                                if (p == 0 && bit == 7)
                                {
                                    continue;
                                }

                                int left;
                                if (bit == 7)
                                {
                                    // use bit #0 from previous byte
                                    left = actline[p - 1] & 1;
                                }
                                else
                                {
                                    // use "previous" bit
                                    left = (actline[p] >> (bit + 1)) & 1;
                                }

                                if (((sub + left) & 1) == 0)
                                {
                                    // reset bit
                                    actline[p] &= (byte)~(1 << bit);
                                }
                                else
                                {
                                    // set bit
                                    actline[p] |= (byte)(1 << bit);
                                }
                            }
                        }
                    }
                    else
                    {
                        // everything else, i.e. bpc 2 and 4, but has been tested for bpc 1 and 8 too
                        int elements = columns * colors;
                        for (int p = colors; p < elements; ++p)
                        {
                            int bytePosSub = p * bitsPerComponent / 8;
                            int bitPosSub = 8 - p * bitsPerComponent % 8 - bitsPerComponent;
                            int bytePosLeft = (p - colors) * bitsPerComponent / 8;
                            int bitPosLeft = 8 - (p - colors) * bitsPerComponent % 8 - bitsPerComponent;
                            
                            int sub = GetBitSeq(actline[bytePosSub], bitPosSub, bitsPerComponent);
                            int left = GetBitSeq(actline[bytePosLeft], bitPosLeft, bitsPerComponent);
                            actline[bytePosSub] = (byte)CalcSetBitSeq(actline[bytePosSub], bitPosSub, bitsPerComponent, sub + left);
                        }
                    }
                    break;

                case 10:
                    // PRED NONE
                    // do nothing
                    break;

                case 11:
                    // PRED SUB
                    for (int p = bytesPerPixel; p < rowLength; p++)
                    {
                        int sub = actline[p];
                        int left = actline[p - bytesPerPixel];
                        actline[p] = (byte)(sub + left);
                    }
                    break;

                case 12:
                    // PRED UP
                    for (int p = 0; p < rowLength; p++)
                    {
                        int up = actline[p] & 0xff;
                        int prior = lastline[p] & 0xff;
                        actline[p] = (byte)((up + prior) & 0xff);
                    }
                    break;

                case 13:
                    // PRED AVG
                    for (int p = 0; p < rowLength; p++)
                    {
                        int avg = actline[p] & 0xff;
                        int left = p - bytesPerPixel >= 0 ? actline[p - bytesPerPixel] & 0xff : 0;
                        int up = lastline[p] & 0xff;
                        actline[p] = (byte)((avg + (left + up) / 2) & 0xff);
                    }
                    break;

                case 14:
                    // PRED PAETH
                    for (int p = 0; p < rowLength; p++)
                    {
                        int paeth = actline[p] & 0xff;
                        int a = p - bytesPerPixel >= 0 ? actline[p - bytesPerPixel] & 0xff : 0;
                        int b = lastline[p] & 0xff;
                        int c = p - bytesPerPixel >= 0 ? lastline[p - bytesPerPixel] & 0xff : 0;
                        int value = a + b - c;
                        int absa = Math.Abs(value - a);
                        int absb = Math.Abs(value - b);
                        int absc = Math.Abs(value - c);
                        
                        if (absa <= absb && absa <= absc)
                        {
                            actline[p] = (byte)((paeth + a) & 0xff);
                        }
                        else if (absb <= absc)
                        {
                            actline[p] = (byte)((paeth + b) & 0xff);
                        }
                        else
                        {
                            actline[p] = (byte)((paeth + c) & 0xff);
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        public static int CalculateRowLength(int colors, int bitsPerComponent, int columns)
        {
            int bitsPerPixel = colors * bitsPerComponent;
            return (columns * bitsPerPixel + 7) / 8;
        }

        /// <summary>
        /// Get value from bit interval from a byte.
        /// </summary>
        private static int GetBitSeq(int by, int startBit, int bitSize)
        {
            int mask = ((1 << bitSize) - 1);
            return (by >> startBit) & mask;
        }

        /// <summary>
        /// Set value in a bit interval and return that value.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="startBit"></param>
        /// <param name="bitSize"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        private static int CalcSetBitSeq(int by, int startBit, int bitSize, int val)
        {
            int mask = ((1 << bitSize) - 1);
            int truncatedVal = val & mask;
            mask = ~(mask << startBit);
            return (by & mask) | (truncatedVal << startBit);
        }

        /// <summary>
        /// Wraps a <see cref="Stream"/> in a predictor decoding stream as necessary.
        /// <para>If no predictor is specified by the parameters, the original stream is returned as is.</para>
        /// </summary>
        /// <param name="outStream">The stream to which decoded data should be written.</param>
        /// <param name="predictor"></param>
        /// <param name="colors"></param>
        /// <param name="bitsPerComponent"></param>
        /// <param name="columns"></param>
        /// <returns>A <see cref="Stream"/> is returned, which will write decoded data
        /// into the given stream. If no predictor is specified, the original stream is returned.</returns>
        public static Stream WrapPredictor(Stream outStream, int predictor, int colors, int bitsPerComponent, int columns)
        {
            if (predictor > 1)
            {
                return new PredictorOutputStream(outStream, predictor, colors, bitsPerComponent, columns);
            }

            return outStream;
        }

        /**
         * Output stream that implements predictor decoding. Data is buffered until a complete
         * row is available, which is then decoded and written to the underlying stream.
         * The previous row is retained for decoding the next row.
         */
        private sealed class PredictorOutputStream : Stream
        {
            private readonly Stream _baseStream;
            private int _predictor;
            private readonly int _colors;
            private readonly int _bitsPerComponent;
            private readonly int _columns;
            private readonly int _rowLength;
            private readonly bool _predictorPerRow;
            private byte[] _currentRow;
            private byte[] _lastRow;
            private int _currentRowData = 0;
            private bool _predictorRead = false;

            public PredictorOutputStream(Stream baseStream, int predictor, int colors, int bitsPerComponent, int columns)
            {
                _baseStream = baseStream;
                _predictor = predictor;
                _colors = colors;
                _bitsPerComponent = bitsPerComponent;
                _columns = columns;
                _rowLength = CalculateRowLength(colors, bitsPerComponent, columns);
                _predictorPerRow = predictor >= 10;
                _currentRow = new byte[_rowLength];
                _lastRow = new byte[_rowLength];
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                int currentOffset = offset;
                int maxOffset = currentOffset + count;
                while (currentOffset < maxOffset)
                {
                    if (_predictorPerRow && _currentRowData == 0 && !_predictorRead)
                    {
                        // PNG predictor; each row starts with predictor type (0, 1, 2, 3, 4)
                        // read per line predictor, add 10 to tread value 0 as 10, 1 as 11, ...
                        _predictor = buffer[currentOffset] + 10;
                        currentOffset++;
                        _predictorRead = true;
                    }
                    else
                    {
                        int toRead = Math.Min(_rowLength - _currentRowData, maxOffset - currentOffset);
                        Array.Copy(buffer, currentOffset, _currentRow, _currentRowData, toRead);
                        _currentRowData += toRead;
                        currentOffset += toRead;

                        // current row is filled, decode it, write it to underlying stream,
                        // and reset the state.
                        if (_currentRowData == _currentRow.Length)
                        {
                            DecodeAndWriteRow();
                        }
                    }
                }
            }

            private void DecodeAndWriteRow()
            {
                DecodePredictorRow(_predictor, _colors, _bitsPerComponent, _columns, _currentRow, _lastRow);
                _baseStream.Write(_currentRow, 0, _currentRow.Length);
                FlipRows();
            }

            /**
             * Flips the row buffers (to avoid copying), and resets the current-row index
             * and predictorRead flag
             */
            private void FlipRows()
            {
                (_lastRow, _currentRow) = (_currentRow, _lastRow);
                _currentRowData = 0;
                _predictorRead = false;
            }

            public override void Flush()
            {
                // The last row is allowed to be incomplete, and should be completed with zeros.
                if (_currentRowData > 0)
                {
                    // public static void fill(int[] a, int fromIndex, int toIndex, int val)
                    // Arrays.fill(currentRow, currentRowData, rowLength, (byte)0);
                    _currentRow.AsSpan(_currentRowData, _rowLength - _currentRowData).Fill(byte.MinValue);

                    DecodeAndWriteRow();
                }
                _baseStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException("Read not supported");
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void WriteByte(byte value) => throw new NotSupportedException("Not supported");
        }
    }
}
