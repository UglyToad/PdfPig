namespace UglyToad.PdfPig.Filters
{
    using CcittFax;
    using Fonts;
    using System;
    using Tokens;
    using Util;

    /// <summary>Resolves PDF CCITT parameters and retries rejected bitmap decoding with the legacy filter.</summary>
    /// <remarks>
    /// <para>PDF Reference 1.7, section 3.3.5: EndOfBlock selects RTC/EOFB termination; otherwise
    /// positive Rows or clean EOF stops decoding. Height limits returned pixels only. Extra encoded
    /// rows still update the 2D reference until the end. See <see cref="CcittFaxBitmapDecoder"/> for the algorithm.</para>
    /// <para>After complete rows, corpus-derived EOF tolerances accept shortened Group 3 RTCs
    /// (2–5 EOLs) with unknown or exact Rows. A single EOL or missing marker requires exact positive
    /// Rows. These exceptions require an all-zero tail and valid mixed-mode tags; unknown Rows alone
    /// permit only final-byte zero padding. Height never authorizes termination.</para>
    /// <para>Both paths share a 256 MiB per-call buffer ceiling, including replaced arrays and failed
    /// attempts. Budget failures throw; malformed data restarts from the original input with legacy.
    /// Widths above UInt16 also use legacy. This is not a document-wide memory limit.</para>
    /// <para>Explicit EndOfLine overrides header detection, following Apache PDFBox 3.0.8
    /// (Apache-2.0), PDFBOX-6080. Optional EOL is accepted for MH and Group 4.</para>
    /// </remarks>
    public sealed class CcittFaxDecodeFilter : IFilter
    {
        private static readonly LegacyCcittFaxDecodeFilter Legacy = new LegacyCcittFaxDecodeFilter();

        /// <inheritdoc/>
        public bool IsSupported { get; } = true;

        /// <inheritdoc/>
        public Memory<byte> Decode(Memory<byte> input,
            DictionaryToken streamDictionary,
            IFilterProvider filterProvider,
            int filterIndex) =>
            DecodeWithConsumption(input, streamDictionary, filterProvider, filterIndex, out _, out _);

        /// <summary>Decodes pixels and, when the fast path succeeds, reports the verified encoded boundary.</summary>
        /// <remarks>Lookahead bytes are excluded. Offsets refer to this filter's input, not earlier
        /// filter stages or the PDF file. Legacy retries cannot establish an exact boundary and
        /// report zero consumption with NotCompleted, even when they return decoded pixels.</remarks>
        internal Memory<byte> DecodeWithConsumption(Memory<byte> input,
            DictionaryToken streamDictionary,
            IFilterProvider filterProvider,
            int filterIndex,
            out int bytesConsumed,
            out CcittFaxDecodeStatus status)
        {
            bytesConsumed = 0;
            status = CcittFaxDecodeStatus.NotCompleted;
            var parameters = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);
            var columns = parameters.GetIntOrDefault(NameToken.Columns, 1728);
            var rows = parameters.GetIntOrDefault(NameToken.Rows, 0);
            var height = streamDictionary.GetIntOrDefault(NameToken.Height, NameToken.H, 0);
            if (columns <= 0 || rows < 0)
            {
                throw new CorruptCompressedDataException(
                    $"Invalid CCITT image dimensions: columns={columns}, rows={rows}.");
            }

            // Height controls stored pixels; Rows controls termination only when EndOfBlock is false.
            // Allocate a capacity hint, then keep validating extra rows until the encoded end.
            var endOfBlock = parameters.GetBooleanOrDefault(NameToken.Create("EndOfBlock"), true);
            var maximumStoredRows = Math.Max(0, height);
            var capacityRows = !endOfBlock && rows > 0
                ? maximumStoredRows > 0 ? Math.Min(rows, maximumStoredRows) : rows
                : Math.Max(1, maximumStoredRows);
            var bitmapByteCount = GetDecodedBufferSize(columns, capacityRows);
            // UInt16 cannot represent wider transition positions. Empty input also retains
            // the historical filter behavior without attempting to recognize a new boundary.
            if (columns > ushort.MaxValue || input.IsEmpty)
            {
                return Legacy.Decode(input, streamDictionary, filterProvider, filterIndex);
            }

            var bitmap = new byte[bitmapByteCount];
            var options = new CcittFaxDecodeOptions(columns,
                capacityRows,
                DetermineCompressionType(input.Span, parameters.GetIntOrDefault(NameToken.K, 0), parameters),
                parameters.GetBooleanOrDefault(NameToken.EncodedByteAlign, false),
                parameters.GetBooleanOrDefault(NameToken.BlackIs1, false),
                acceptOptionalEndOfLine: true,
                endOfBlock: endOfBlock,
                requireEndOfLine: parameters.GetBooleanOrDefault(NameToken.EndOfLine, false),
                verifyEndMarker: endOfBlock,
                allowIncompleteTermination: endOfBlock,
                declaredRows: rows,
                allowEndOfInput: !endOfBlock || rows == 0,
                maximumAllocatedBufferBytes: LegacyCcittFaxDecodeFilter.MaximumDecodeBufferBytes,
                maximumStoredRows: maximumStoredRows,
                maximumDecodedRows: endOfBlock ? 0 : rows);
            if (CcittFaxBitmapDecoder.TryDecodeBitmap(input.Span, ref bitmap, in options, out var result))
            {
                bytesConsumed = result.BytesConsumed;
                status = result.Status;
                int storedRows = maximumStoredRows > 0
                    ? Math.Min(result.DecodedRows, maximumStoredRows)
                    : result.DecodedRows;
                return bitmap.AsMemory(0, storedRows * ((columns + 7) / 8));
            }

            // Legacy prefers Height when both dimensions are positive. Reuse the cleared
            // bitmap only if its size matches; charge any replacement to the same budget.
            var legacyRows = rows > 0 && height > 0 ? height : Math.Max(rows, height);
            var legacyBitmapBytes = GetDecodedBufferSize(columns, legacyRows);
            var reuseBitmap = bitmap.Length == legacyBitmapBytes;
            var legacyWorkingBytes = 2L * (columns + 2) * sizeof(int) + ((long)columns + 7) / 8;
            if (result.AllocatedBufferBytes + legacyWorkingBytes + (reuseBitmap ? 0 : legacyBitmapBytes) >
                LegacyCcittFaxDecodeFilter.MaximumDecodeBufferBytes)
            {
                throw new CorruptCompressedDataException("CCITT legacy retry buffers exceed the allocation budget.");
            }

            return Legacy.DecodeInto(input, streamDictionary, filterProvider, filterIndex, reuseBitmap ? bitmap : null);
        }

        /// <summary>Validates the bitmap size against the shared allocation limit before allocating.</summary>
        /// <remarks>Use the legacy working-buffer estimate so the initial dimension check is equally
        /// conservative for both paths. Resizes and retries are additionally budgeted as they occur.</remarks>
        internal static int GetDecodedBufferSize(int columns, int rows) =>
            LegacyCcittFaxDecodeFilter.GetDecodedBufferSize(columns, rows);

        /// <summary>Selects the coding family from K and resolves MH versus Group 3 for K=0.</summary>
        /// <remarks>Positive K selects mixed Group 3; negative K selects Group 4. For K=0, an explicit
        /// EndOfLine overrides detection. If omitted, search at most 20 input bytes for EOL, retaining
        /// the legacy heuristic used for PDFs whose framing parameters are incomplete.</remarks>
        private static CcittFaxCompressionType DetermineCompressionType(ReadOnlySpan<byte> input,
            int k,
            DictionaryToken decodeParms)
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
                        eolWindow = (short)((eolWindow << 1) +
                                            ((input[(bitIndex / 8)] >> (7 - (bitIndex % 8))) & 0x01));
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