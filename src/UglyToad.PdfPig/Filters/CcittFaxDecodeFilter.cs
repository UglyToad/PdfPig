namespace UglyToad.PdfPig.Filters
{
    using CcittFax;
    using Fonts;
    using System;
    using Tokens;
    using Util;

    /// <summary>Resolves PDF CCITT parameters, decodes a bitmap and retries rejected fast-path input with the legacy filter.</summary>
    /// <remarks>
    /// <para>This class coordinates decoding: resolve the PDF dictionary, allocate bounded output,
    /// run the fast decoder, then return its bitmap or retry rejected input with the legacy filter.
    /// For the run-length and reference-row algorithm, start with
    /// <see cref="CcittFaxBitmapDecoder"/>; the rules below explain PDF framing and recovery.</para>
    /// <para>PDF Reference 1.7 section 3.3.5 separates filter termination from image dimensions:
    /// EndOfBlock=true stops at RTC (six Group 3 EOLs) or EOFB (two Group 4 EOLs), independently
    /// of Rows. EndOfBlock=false uses positive Rows or clean EOF. Height limits stored pixels,
    /// but cannot identify the encoded end because real PDFs can understate image height.
    /// Extra rows use a scratch row and retain their 2D reference state until termination.</para>
    /// <para>EOL is eleven zero bits followed by one, with optional leading zero fill.
    /// Mixed Group 3 appends a 1D/2D tag to row EOLs; RTC tags must all select 1D.
    /// Explicit EndOfLine takes precedence over header detection, following PDFBox 3.0.8
    /// (Apache-2.0), PDFBOX-6080. Optional EOL is accepted for MH and Group 4.</para>
    /// <para>Corpus-derived termination tolerances apply only after complete, unrepaired rows:
    /// two to five Group 3 RTC EOLs require unknown or exactly matched Rows; a single Group 3/4
    /// EOL or missing marker requires exactly positive Rows. All remaining bits must be zero.
    /// Height cannot authorize these exceptions. Unknown Rows otherwise permits only zero to
    /// seven final-byte zero bits. These tolerances preserve real images but are not full standard
    /// markers: all-zero fill cannot be distinguished from a truncated additional code.</para>
    /// <para>Fast-path termination decisions below apply at boundaries between complete rows.
    /// For EndOfBlock=true, try a complete marker first, then an authorized short marker or EOF;
    /// otherwise continue decoding. Height never authorizes a stopping condition.</para>
    /// <list type="table">
    /// <listheader><term>Condition</term><description>Decision</description></listheader>
    /// <item><term>EndOfBlock=false, positive Rows reached</term><description>Stop with RowLimit;
    /// no RTC/EOFB is required. Clean EOF after complete rows is also accepted.</description></item>
    /// <item><term>EndOfBlock=false, Rows unspecified</term><description>Continue until clean EOF:
    /// only zero to seven trailing zero bits may remain.</description></item>
    /// <item><term>EndOfBlock=true, complete RTC/EOFB</term><description>Stop with EndOfBlock.
    /// Rows and Height need not match the decoded count. Mixed Group 3 RTC tags must select 1D;
    /// bytes after the marker are not inspected as an EOF trailer.</description></item>
    /// <item><term>EndOfBlock=true, two to five Group 3 EOLs</term><description>Accept
    /// IncompleteEndOfBlock only after at least one complete row, with Rows unspecified or
    /// exactly matched, valid mixed-mode tags, and exclusively zero bits through EOF.</description></item>
    /// <item><term>EndOfBlock=true, one Group 3/4 EOL</term><description>Accept IncompleteEndOfBlock
    /// only after exactly positive Rows complete, with a valid tag where applicable and exclusively
    /// zero bits through EOF. Byte-alignment padding must also be zero.</description></item>
    /// <item><term>EndOfBlock=true, missing marker</term><description>Accept EndOfInput after exactly
    /// positive Rows complete and exclusively zero bits through EOF. With Rows unspecified,
    /// accept only zero to seven trailing zero bits after at least one complete row.</description></item>
    /// <item><term>No accepted boundary</term><description>Attempt the next row. Invalid or
    /// truncated row data rejects the fast attempt and triggers a legacy retry; it is not silently
    /// treated as an image ending. Invalid RTC tags cannot authorize a short-marker exception.</description></item>
    /// </list>
    /// <para>Both decoders retain a 256 MiB buffer ceiling. Replaced bitmap allocations and
    /// failed fast-path buffers are counted before any legacy retry. This is a per-call buffer
    /// bound, not a document-wide memory limit. Budget failures are propagated: retrying with
    /// another decoder must not bypass the limit.</para>
    /// <para>Legacy decoding remains available for widths above UInt16 and malformed input
    /// whose historical recovery rules differ from the fast path. Retries start at the original
    /// input position so partially decoded rows never mix the two implementations.</para>
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

            // Legacy uses Height when both Rows and Height are positive. Retain that recovery
            // rule when retrying, even though the fast path separates storage from termination.
            // The bitmap is
            // already cleared. Reuse it if its capacity matches; otherwise budget a fresh one.
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