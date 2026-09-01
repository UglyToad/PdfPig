namespace UglyToad.PdfPig.Filters
{
    using System;
    using UglyToad.PdfPig.Tokens;
#if NET || NETSTANDARD2_1_OR_GREATER
    using System.IO;
    using System.IO.Compression;
    using Core;
    using Fonts;
    using Util;
#endif

    /// <summary>
    /// Brotli (IETF RFC 7932) is a general-purpose, lossless compression algorithm. The Brotli filter decodes
    /// data that has been encoded using Brotli compression. It may be cascaded with a predictor in the same way
    /// as the <see cref="FlateFilter"/> and <see cref="LzwFilter"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The filter is defined by "Brotli compression in PDF 2.0" (EXTN-BROTLI-1, PDF Association), an extension
    /// to ISO 32000. The extension adds BrotliDecode to Clause 7.4.1, Table 6 - Standard Filters, and extends
    /// the LZWDecode and FlateDecode predictor parameters of Clause 7.4.4.3 to cover it, which is why the
    /// predictor handling here matches <see cref="FlateFilter"/>.
    /// </para>
    /// <para>
    /// The stream carries no header of its own: unlike the zlib wrapper the Flate filter has to step over, a
    /// BrotliDecode stream is the raw RFC 7932 bitstream. The extension also forbids the framing format and
    /// shared dictionaries of IETF RFC 9841, and forbids BrotliDecode for inline images.
    /// </para>
    /// <para>
    /// Large window Brotli (IETF RFC 9841), which the extension requires, is <b>not</b> supported. Decoding
    /// uses <c>BrotliStream</c>, whose decoder tops out at the RFC 7932 window of 2^24 bytes and offers
    /// no way to opt in to the larger windows; a stream that uses one is reported as corrupt. The C# decoder in
    /// the reference implementation at https://github.com/google/brotli carries the same 2^24 limit, so
    /// replacing the decoder with it would not lift this restriction - only the reference C implementation
    /// does, through BROTLI_DECODER_PARAM_LARGE_WINDOW. In practice a larger window only pays off for streams
    /// above 16MB.
    /// </para>
    /// </remarks>
    public sealed class BrotliFilter : IFilter
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        // Defaults are from ISO 32000, Clause 7.4.4.3, Table 8, shared with the Flate/LZW predictors.
        private const int DefaultColors = 1;
        private const int DefaultBitsPerComponent = 8;
        private const int DefaultColumns = 1;

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            var parameters = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            var predictor = parameters.GetIntOrDefault(NameToken.Predictor, -1);
            var colors = Math.Min(parameters.GetIntOrDefault(NameToken.Colors, DefaultColors), 32);
            var bitsPerComponent = parameters.GetIntOrDefault(NameToken.BitsPerComponent, DefaultBitsPerComponent);
            var columns = parameters.GetIntOrDefault(NameToken.Columns, DefaultColumns);

            using var memoryStream = MemoryHelper.AsReadOnlyMemoryStream(input);

            try
            {
                using var brotli = new BrotliStream(memoryStream, CompressionMode.Decompress);
                using var output = new MemoryStream((int)(input.Length * 1.5));
                using var f = PngPredictor.WrapPredictor(output, predictor, colors, bitsPerComponent, columns);

                brotli.CopyTo(f);
                f.Flush();

                return output.AsMemory();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is InvalidDataException)
            {
                // InvalidOperationException is what BrotliStream raises for a bitstream it cannot read,
                // including one using the large window this filter does not support. InvalidDataException is
                // caught alongside it because the documented contract of the decompressing streams allows for
                // it and older runtimes have used it.
                throw new CorruptCompressedDataException(
                    "Invalid Brotli compressed stream encountered. A stream using large window Brotli "
                    + "(IETF RFC 9841) fails in the same way, as the decoder does not support it.",
                    ex);
            }
        }
#else
        /// <inheritdoc />
        public bool IsSupported { get; } = false;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            throw new NotSupportedException(
                "The BrotliDecode filter is only supported on .NET Standard 2.1, .NET Core and .NET 5.0 or greater targets.");
        }
#endif
    }
}
