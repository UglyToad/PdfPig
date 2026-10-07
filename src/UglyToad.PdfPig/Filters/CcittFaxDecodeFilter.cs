namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using CcittFax;
    using Core;
    using Fonts;
    using Tokens;
    using Util;

    // Filter updated from original port because of issue #982

    /// <summary>
    /// Decodes image data that has been encoded using either Group 3 or Group 4.
    /// <para>
    /// Ported from https://github.com/apache/pdfbox/blob/714156a15ea6fcfe44ac09345b01e192cbd74450/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxFilter.java
    /// </para>
    /// <para>
    /// Dimension validation, bounded header detection and EndOfLine handling were updated
    /// against PDFBox 3.0.8 CCITTFaxFilter. PdfPig additionally bounds decoder work arrays
    /// and respects parsing leniency for invalid dimensions and empty input.
    /// </para>
    /// </summary>
    public sealed class CcittFaxDecodeFilter : IFilter
    {
        // Like PDFBox's CCITT cap, but includes the decoder's row and change arrays as well
        // as the bitmap. PDF dimensions must not control an unbounded allocation.
        internal const long MaximumDecodeBufferBytes = 256L * 1024 * 1024;

        internal bool UseLenientParsing { get; }

        /// <summary>Creates a CCITT filter with the default lenient dimension handling.</summary>
        public CcittFaxDecodeFilter() : this(useLenientParsing: true)
        {
        }

        internal CcittFaxDecodeFilter(bool useLenientParsing)
        {
            UseLenientParsing = useLenientParsing;
        }

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input,
            DictionaryToken streamDictionary,
            IFilterProvider filterProvider,
            int filterIndex)
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

            // Validate before inspecting the input or constructing the decoder: its constructor
            // allocates arrays from Columns even when the compressed data is empty.
            var arraySize = GetDecodedBufferSize(cols, rows, UseLenientParsing);

            if (cols <= 0 || rows <= 0)
            {
                // Lenient parsing accepts unusable dimensions as empty image data. The
                // resource check above still bounds work buffers described by positive Columns.
                return Memory<byte>.Empty;
            }

            var k = decodeParms.GetIntOrDefault(NameToken.K, 0);
            var encodedByteAlign = decodeParms.GetBooleanOrDefault(NameToken.EncodedByteAlign, false);
            if (input.IsEmpty)
            {
                if (UseLenientParsing)
                {
                    return Memory<byte>.Empty;
                }

                throw new CorruptCompressedDataException("Empty CCITT compressed data.");
            }

            var compressionType = DetermineCompressionType(input.Span, k, decodeParms);

            using (var stream = new CcittFaxDecoderStream(MemoryHelper.AsReadOnlyMemoryStream(input), cols, compressionType, encodedByteAlign))
            {
                var decompressed = new byte[arraySize];
                ReadFromDecoderStream(stream, decompressed);

                // we expect black to be 1, if not invert the bitmap 
                var blackIsOne = decodeParms.GetBooleanOrDefault(NameToken.BlackIs1, false);
                if (!blackIsOne)
                {
                    InvertBitmap(decompressed);
                }

                return decompressed;
            }
        }

        /// <summary>Validates all dimension-dependent decode buffers before any are allocated.</summary>
        internal static int GetDecodedBufferSize(int columns, int rows, bool useLenientParsing = false)
        {
            if (!useLenientParsing && (columns <= 0 || rows <= 0))
            {
                throw new CorruptCompressedDataException($"Invalid CCITT image dimensions: columns={columns}, rows={rows}.");
            }

            // Nonpositive dimensions cannot contribute bytes, and must not cancel positive
            // sizes in lenient mode. Widen before adding or multiplying; Int32 dimensions
            // keep all of these computations within Int64.
            var safeColumns = Math.Max(0L, columns);
            var safeRows = Math.Max(0L, rows);
            var rowBytes = (safeColumns + 7) / 8;
            var bitmapBytes = rowBytes * safeRows;
            var workingBytes = rowBytes + 2 * (safeColumns + 2) * sizeof(int);
            var totalBytes = bitmapBytes + workingBytes;

            if (totalBytes > MaximumDecodeBufferBytes)
            {
                throw new CorruptCompressedDataException(
                    $"CCITT decode buffers require {totalBytes} bytes for columns={columns}, rows={rows}; "
                    + $"at most {MaximumDecodeBufferBytes} bytes are allowed.");
            }

            // The total cap also guarantees that the decoder's Int32 array lengths cannot overflow.
            return (int)bitmapBytes;
        }

        private static CcittFaxCompressionType DetermineCompressionType(ReadOnlySpan<byte> input, int k, DictionaryToken decodeParms)
        {
            if (k == 0)
            {
                if (decodeParms.ContainsKey(NameToken.EndOfLine))
                {
                    // PDFBOX-6080: an explicit EndOfLine parameter takes precedence over sniffing.
                    return decodeParms.GetBooleanOrDefault(NameToken.EndOfLine, false)
                        ? CcittFaxCompressionType.Group3_1D
                        : CcittFaxCompressionType.ModifiedHuffman;
                }

                var compressionType = CcittFaxCompressionType.Group3_1D; // Group 3 1D

                if (input.Length < 2 || input[0] != 0 || (input[1] >> 4 != 1 && input[1] != 1))
                {
                    // leading EOL (0b000000000001) not found, search further and
                    // try RLE if not found
                    compressionType = CcittFaxCompressionType.ModifiedHuffman;
                    var secondByte = input.Length > 1 ? input[1] : 0;
                    var b = (short)(((input[0] << 8) + secondByte) >> 4);
                    var headerBits = Math.Min(input.Length, 20) * 8;
                    for (var i = 12; i < headerBits; i++)
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
        }

        internal static void InvertBitmap(Span<byte> bufferData)
        {
            var i = 0;
            if (Vector.IsHardwareAccelerated && bufferData.Length >= Vector<byte>.Count)
            {
                // Invert complete vectors in place; preserve the scalar path for the tail
                // and runtimes without hardware acceleration.
                var vectors = MemoryMarshal.Cast<byte, Vector<byte>>(bufferData);
                var mask = new Vector<byte>(byte.MaxValue);
                for (var v = 0; v < vectors.Length; v++)
                {
                    vectors[v] ^= mask;
                }
                i = vectors.Length * Vector<byte>.Count;
            }

            for (; i < bufferData.Length; i++)
            {
                bufferData[i] = (byte)~bufferData[i];
            }
        }
    }
}