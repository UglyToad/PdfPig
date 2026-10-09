namespace UglyToad.PdfPig.Filters
{
    using System;
    using CcittFax;
    using Core;
    using Fonts;
    using Tokens;
    using Util;

    // PDFBox 3.0.8 source used for dimension validation, bounded EOL detection and EndOfLine handling:
    // https://github.com/apache/pdfbox/blob/3.0.8/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxFilter.java
    // Apache-2.0. The allocation budget and strict/lenient policy below are PdfPig additions.

    /// <summary>
    /// Decodes CCITT image data using Modified Huffman, Group 3 or Group 4 row formats.
    /// <para>
    /// Originally ported from https://github.com/apache/pdfbox/blob/714156a15ea6fcfe44ac09345b01e192cbd74450/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxFilter.java
    /// </para>
    /// <para>
    /// Parameter validation and header detection follow PDFBox 3.0.8. PdfPig also limits
    /// dimension-dependent decode buffers and applies the document's parsing mode.
    /// </para>
    /// </summary>
    public sealed class CcittFaxDecodeFilter : IFilter
    {
        // Per-decode size limit, estimated as the bitmap plus one row and two Int32 transition arrays.
        // This conservatively covers either row representation, but is not a limit on total process
        // memory or all streams in a document. Static tables and object/array headers are excluded.
        // Lenient parsing can recover from invalid data; it cannot bypass this allocation limit.
        internal const long MaximumDecodeBufferBytes = 256L * 1024 * 1024;

        internal bool UseLenientParsing { get; }

        /// <summary>Creates a lenient CCITT filter. The allocation budget remains enforced.</summary>
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

            var columns = decodeParms.GetIntOrDefault(NameToken.Columns, 1728);
            var rowCount = decodeParms.GetIntOrDefault(NameToken.Rows, 0);
            var imageHeight = streamDictionary.GetIntOrDefault(NameToken.Height, NameToken.H, 0);
            if (rowCount > 0 && imageHeight > 0)
            {
                // Prefer image Height when both values are positive: DecodeParms Rows can
                // be incorrect in real PDFs (PDFBOX-771, PDFBOX-3727).
                rowCount = imageHeight;
            }
            else
            {
                // Use whichever value is positive; validation below handles missing or invalid values.
                rowCount = Math.Max(rowCount, imageHeight);
            }

            // Validate effective dimensions and the buffer estimate before inspecting or decoding input.
            // Empty input and lenient parsing still go through the same allocation guard.
            var bitmapByteCount = GetDecodedBufferSize(columns, rowCount, UseLenientParsing);

            if (columns <= 0 || rowCount <= 0)
            {
                // Strict parsing has already rejected these dimensions. Lenient parsing returns
                // empty data after checking that a positive Columns value cannot bypass the limit.
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

            var decodedBitmap = new byte[bitmapByteCount];
            var blackIsOne = decodeParms.GetBooleanOrDefault(NameToken.BlackIs1, false);
            CcittFaxCompactDecoder.Decode(input.Span, decodedBitmap, columns, rowCount, compressionType, encodedByteAlign, blackIsOne, UseLenientParsing);
            return decodedBitmap;
        }
        /// <summary>Checks dimensions and the buffer budget, then returns the bitmap byte count.</summary>
        internal static int GetDecodedBufferSize(int columns, int rowCount, bool useLenientParsing = false)
        {
            if (!useLenientParsing && (columns <= 0 || rowCount <= 0))
            {
                throw new CorruptCompressedDataException($"Invalid CCITT image dimensions: columns={columns}, rows={rowCount}.");
            }

            // Clamp dimensions separately: negative Rows must not cancel the work-array estimate
            // for positive Columns in lenient mode. Convert to Int64 before arithmetic; even the
            // largest Int32 dimensions keep this complete estimate within Int64.
            var nonnegativeColumns = Math.Max(0L, columns);
            var nonnegativeRows = Math.Max(0L, rowCount);
            var rowByteCount = (nonnegativeColumns + 7) / 8;
            var bitmapByteCount = rowByteCount * nonnegativeRows;
            var workingBufferByteCount = rowByteCount + 2 * (nonnegativeColumns + 2) * sizeof(int);
            var totalBufferByteCount = bitmapByteCount + workingBufferByteCount;

            if (totalBufferByteCount > MaximumDecodeBufferBytes)
            {
                throw new CorruptCompressedDataException(
                    $"CCITT decode buffers require {totalBufferByteCount} bytes for columns={columns}, rows={rowCount}; "
                    + $"at most {MaximumDecodeBufferBytes} bytes are allowed.");
            }

            // Passing the total limit ensures bitmap and transition-array lengths also fit in Int32.
            return (int)bitmapByteCount;
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

                var compressionType = CcittFaxCompressionType.Group3_1D;

                if (input.Length < 2 || input[0] != 0 || (input[1] >> 4 != 1 && input[1] != 1))
                {
                    // No leading end-of-line (EOL) code was found. Search the first 20 input
                    // bytes for 000000000001; otherwise use Modified Huffman without row EOLs.
                    compressionType = CcittFaxCompressionType.ModifiedHuffman;
                    var secondByte = input.Length > 1 ? input[1] : 0;
                    var eolWindow = (short)(((input[0] << 8) + secondByte) >> 4);
                    var headerBitCount = Math.Min(input.Length, 20) * 8;
                    for (var bitIndex = 12; bitIndex < headerBitCount; bitIndex++)
                    {
                        eolWindow = (short)((eolWindow << 1) + ((input[(bitIndex / 8)] >> (7 - (bitIndex % 8))) & 0x01));
                        if ((eolWindow & 0xFFF) == 1)
                        {
                            return CcittFaxCompressionType.Group3_1D;
                        }
                    }
                }

                return compressionType;
            }

            if (k > 0)
            {
                return CcittFaxCompressionType.Group3_2D;
            }

            return CcittFaxCompressionType.Group4_2D;
        }

    }
}
