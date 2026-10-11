namespace UglyToad.PdfPig.Filters
{
    using System;
    using CcittFax;
    using Core;
    using Fonts;
    using Tokens;
    using Util;

    /// <summary>Decodes CCITT using the original row-by-row stream implementation.</summary>
    /// <remarks>
    /// <para>Available both as a caller-selected filter and as the automatic fallback when the
    /// bitmap decoder rejects input. Retains the historical choice of image Height over positive
    /// Rows, fixed-size output and malformed-row recovery. It does not report a verified RTC/EOFB
    /// boundary or input consumption; the caller must not infer those from its returned pixels.</para>
    /// <para>Ported from the Apache-2.0
    /// <see href="https://github.com/apache/pdfbox/blob/714156a15ea6fcfe44ac09345b01e192cbd74450/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxFilter.java">PDFBox CCITTFaxFilter</see>.
    /// Dimension-based allocations remain subject to the shared 256 MiB decoder-buffer limit.</para>
    /// </remarks>
    public sealed class LegacyCcittFaxDecodeFilter : IFilter
    {
        internal const long MaximumDecodeBufferBytes = 256L * 1024 * 1024;
        internal const int HeaderSniffByteCount = 20;

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input,
            DictionaryToken streamDictionary,
            IFilterProvider filterProvider,
            int filterIndex)
        {
            return DecodeInto(input, streamDictionary, filterProvider, filterIndex, null);
        }

        /// <summary>Decodes with the original stream decoder, optionally reusing a bitmap from a failed fast attempt.</summary>
        /// <remarks>A supplied bitmap must have exactly the resolved image size. The caller must
        /// clear it before retrying: the legacy decoder can leave unread output bytes untouched.
        /// A null bitmap requests a new zero-initialized array. Reusing the fast attempt's bitmap
        /// avoids allocating a second image during fallback.</remarks>
        internal Memory<byte> DecodeInto(Memory<byte> input,
            DictionaryToken streamDictionary,
            IFilterProvider filterProvider,
            int filterIndex,
            byte[]? bitmap)
        {
            var decodeParms = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            var cols = decodeParms.GetIntOrDefault(NameToken.Columns, 1728);
            var rows = decodeParms.GetIntOrDefault(NameToken.Rows, 0);
            var height = streamDictionary.GetIntOrDefault(NameToken.Height, NameToken.H, 0);
            if (rows > 0 && height > 0)
            {
                // PDFBOX-771, PDFBOX-3727: rows in DecodeParms sometimes contains an incorrect value
                rows = height;
            }
            else
            {
                // at least one of the values has to have a valid value
                rows = Math.Max(rows, height);
            }

            var arraySize = GetDecodedBufferSize(cols, rows);
            if (bitmap != null && bitmap.Length != arraySize)
            {
                throw new ArgumentException("The bitmap length must match the resolved CCITT image size.",
                    nameof(bitmap));
            }

            var k = decodeParms.GetIntOrDefault(NameToken.K, 0);
            var encodedByteAlign = decodeParms.GetBooleanOrDefault(NameToken.EncodedByteAlign, false);
            var compressionType = DetermineCompressionType(input.Span, k);

            using (var stream = new CcittFaxDecoderStream(MemoryHelper.AsReadOnlyMemoryStream(input),
                       cols,
                       compressionType,
                       encodedByteAlign))
            {
                var decompressed = bitmap ?? new byte[arraySize];
                ReadFromDecoderStream(stream, decompressed);

                // The stream decoder always emits black as one; PDF BlackIs1 defaults to false.
                var blackIsOne = decodeParms.GetBooleanOrDefault(NameToken.BlackIs1, false);
                if (!blackIsOne)
                {
                    InvertBitmap(decompressed);
                }

                return decompressed;
            }
        }

        /// <summary>Checks dimensions and returns the packed bitmap size before any decoder arrays are allocated.</summary>
        /// <remarks>Count the bitmap, one decoded row and two Int32 transition arrays in widened
        /// arithmetic. A small compressed stream can declare huge dimensions; rejecting that size
        /// after allocation would already expose the process to memory exhaustion.</remarks>
        internal static int GetDecodedBufferSize(int columns, int rows)
        {
            if (columns <= 0 || rows <= 0)
            {
                throw new CorruptCompressedDataException(
                    $"Invalid CCITT image dimensions: columns={columns}, rows={rows}.");
            }

            // PDFBOX-6243: cap the bitmap, decoded row and both change arrays at 256 MiB.
            // Widen before arithmetic and validate before constructing the decoder.
            // https://github.com/apache/pdfbox/blob/3.0/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxFilter.java
            var bitmapBytes = ((long)columns + 7) / 8 * rows;
            var transitionArrayBytes = ((long)columns + 2) * sizeof(int) * 2;
            var decodedRowBytes = ((long)columns + 7) / 8;
            var workingBufferBytes = transitionArrayBytes + decodedRowBytes;
            if (bitmapBytes + workingBufferBytes > MaximumDecodeBufferBytes)
            {
                throw new CorruptCompressedDataException(
                    $"CCITT decode buffers require {bitmapBytes + workingBufferBytes} bytes for columns={columns}, rows={rows}; "
                    + $"at most {MaximumDecodeBufferBytes} bytes are allowed.");
            }

            return (int)bitmapBytes;
        }

        /// <summary>Retains the original K selection and, for K=0, the 20-byte EOL detection heuristic.</summary>
        /// <remarks>This legacy detector intentionally keeps its historical input assumptions.
        /// The fast filter's detector separately handles short input and explicit EndOfLine.</remarks>
        internal static CcittFaxCompressionType DetermineCompressionType(ReadOnlySpan<byte> input, int k)
        {
            if (k == 0)
            {
                var compressionType = CcittFaxCompressionType.Group3_1D; // Group 3 1D

                if (input[0] != 0 || (input[1] >> 4 != 1 && input[1] != 1))
                {
                    // leading EOL (0b000000000001) not found, search further and
                    // try RLE if not found
                    compressionType = CcittFaxCompressionType.ModifiedHuffman;
                    var b = (short)(((input[0] << 8) + (input[1] & 0xff)) >> 4);
                    for (var i = 12; i < HeaderSniffByteCount * 8; i++)
                    {
                        b = (short)((b << 1) + ((input[(i / 8)] >> (7 - (i % 8))) & 0x01));
                        if ((b & 0xFFF) == 1)
                        {
                            return CcittFaxCompressionType.Group3_1D;
                        }
                    }
                }

                return compressionType;
            }

            if (k > 0)
            {
                // Group 3 2D
                return CcittFaxCompressionType.Group3_2D;
            }

            return CcittFaxCompressionType.Group4_2D;
        }

        /// <summary>Fills the resolved image buffer or stops when the stream decoder reports exhausted input.</summary>
        /// <remarks>Output length follows the resolved dimensions. Unread bytes retain their initial
        /// zero values; this is why a reused bitmap must be cleared before the legacy retry.</remarks>
        private static void ReadFromDecoderStream(CcittFaxDecoderStream decoderStream, byte[] result)
        {
            var pos = 0;
            int read;
            while ((read = decoderStream.Read(result, pos, result.Length - pos)) > -1)
            {
                pos += read;
                if (pos >= result.Length)
                {
                    break;
                }
            }

            decoderStream.Close();
        }

        /// <summary>Converts the stream decoder's black-one bitmap to the PDF filter's requested polarity.</summary>
        private static void InvertBitmap(Span<byte> bufferData)
        {
            for (int i = 0; i < bufferData.Length; i++)
            {
                ref byte b = ref bufferData[i];
                b = (byte)(~b & 0xFF);
            }
        }
    }
}