namespace UglyToad.PdfPig.Filters
{
    using System;
    using CcittFax;
    using Core;
    using Fonts;
    using Tokens;
    using Util;

    /// <summary>Resolves PDF CCITTFaxDecode parameters and decodes image data into a bounded bitmap.</summary>
    /// <remarks>
    /// <para>Resolve Columns and the effective Rows/Height before allocating. K selects Group 4,
    /// mixed Group 3, or 1D coding; for K = 0, explicit EndOfLine or bounded header inspection selects
    /// EOL-synchronized Group 3 versus Modified Huffman without per-row EOL. EncodedByteAlign and
    /// BlackIs1 are passed to <see cref="CcittFaxCompactDecoder"/>.</para>
    /// <para>The 256 MiB per-call buffer estimate includes the bitmap, one row and two Int32 transition
    /// arrays. It excludes object headers and static lookup tables and is not a document-wide or
    /// process-memory limit. Strict parsing rejects nonpositive dimensions and empty input; lenient
    /// parsing can return empty output. Neither parsing mode bypasses the allocation limit.</para>
    /// <para>Implementation provenance:</para>
    /// <list type="table">
    /// <listheader><term>Source</term><description>Reused or added behavior</description></listheader>
    /// <item><term>Original Apache PDFBox port (Apache-2.0)</term><description>PDF parameter resolution
    /// and CCITT filter structure. Original attribution:
    /// <see href="https://github.com/apache/pdfbox/blob/714156a15ea6fcfe44ac09345b01e192cbd74450/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxFilter.java">CCITTFaxFilter</see>.</description></item>
    /// <item><term>PDFBox 3.0.8 (Apache-2.0)</term><description>Effective image height, positive-dimension
    /// validation, explicit EndOfLine handling and bounded EOL header detection.
    /// See <see href="https://github.com/apache/pdfbox/blob/3.0.8/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxFilter.java">release filter</see>.
    /// PdfPig applies its own lenient policy to invalid dimensions.</description></item>
    /// <item><term>PdfPig (Apache-2.0)</term><description>Int64 buffer-size arithmetic, the per-call
    /// allocation ceiling, document-scoped strict/lenient configuration and compact bitmap decoding.</description></item>
    /// </list>
    /// </remarks>
    public sealed class CcittFaxDecodeFilter : IFilter
    {
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
                rowCount = Math.Max(rowCount, imageHeight);
            }

            // Validate before empty-input and lenient recovery can return.
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
