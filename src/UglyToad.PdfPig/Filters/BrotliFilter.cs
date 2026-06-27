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
    /// Brotli (RFC 7932) is a general-purpose, lossless compression algorithm. The Brotli filter decodes data
    /// that has been encoded using Brotli compression. It may be cascaded with a predictor in the same way as
    /// the <see cref="FlateFilter"/> and <see cref="LzwFilter"/>.
    /// </summary>
    public sealed class BrotliFilter : IFilter
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        // Defaults are from table 3.7 in the spec (version 1.7), shared with the Flate/LZW predictors.
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
            catch (InvalidDataException ex)
            {
                throw new CorruptCompressedDataException("Invalid Brotli compressed stream encountered", ex);
            }
        }
#else
        /// <inheritdoc />
        public bool IsSupported { get; } = false;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            throw new NotSupportedException(
                "The BrotliDecode filter is only supported on .NET Standard 2.1, .NET Core and .NET 6.0 or greater targets.");
        }
#endif
    }
}
