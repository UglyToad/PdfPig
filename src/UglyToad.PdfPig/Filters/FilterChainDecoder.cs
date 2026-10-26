namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Tokens;

    /// <summary>Applies the existing chain estimates inside built-in decoders without changing IFilter.</summary>
    internal static class FilterChainDecoder
    {
        public static Memory<byte> Decode(Memory<byte> input, DictionaryToken dictionary,
            IFilterProvider provider, IReadOnlyList<IFilter> filters, int maximumOutputLength = DecodeBuffer.MaximumCapacity)
        {
            double estimatedLimit = 100d * input.Length;
            var decoded = input;
            for (var i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                estimatedLimit *= GetEstimatedSizeMultiplier(filter);
                // Preserve the existing ratio policy: it applies only to intermediate stages.
                var checkRatio = i < filters.Count - 1;
                var outputLimit = checkRatio
                    ? (int)Math.Min(maximumOutputLength, estimatedLimit)
                    : maximumOutputLength;
                var builtIn = filter is FlateFilter || filter is LzwFilter || filter is BrotliFilter
                    || filter is RunLengthFilter || filter is Ascii85Filter;
                try
                {
                    decoded = filter switch
                    {
                        Ascii85Filter ascii85 => ascii85.Decode(decoded, dictionary, provider, i, outputLimit),
                        FlateFilter flate => flate.Decode(decoded, dictionary, provider, i, outputLimit),
                        LzwFilter lzw => lzw.Decode(decoded, dictionary, provider, i, outputLimit),
                        BrotliFilter brotli => brotli.Decode(decoded, dictionary, provider, i, outputLimit),
                        RunLengthFilter runLength => runLength.Decode(decoded, dictionary, provider, i, outputLimit),
                        _ => filter.Decode(decoded, dictionary, provider, i)
                    };
                }
                catch (DecodeBuffer.LimitExceededException ex) when
                    (builtIn && checkRatio && estimatedLimit < maximumOutputLength)
                {
                    // Retain the public exception type and message prefix of the old ratio guard.
                    throw new PdfDocumentFormatException(
                        $"Decoded stream size exceeds the estimated maximum size. Output limit: {outputLimit}, {i + 1} filters applied out of {filters.Count}.", ex);
                }

                // Keep the existing fallback for custom filters and the other built-in filters.
                if (!builtIn && checkRatio && decoded.Length > estimatedLimit)
                {
                    throw new PdfDocumentFormatException(
                        $"Decoded stream size exceeds the estimated maximum size. Current decoded stream length: {decoded.Length}, {i + 1} filters applied out of {filters.Count}.");
                }
            }
            return decoded;
        }

        private static double GetEstimatedSizeMultiplier(IFilter filter) => filter switch
        {
            AsciiHexDecodeFilter => 0.5,
            Ascii85Filter => 0.8,
            RunLengthFilter => 1.5,
            LzwFilter => 2,
            FlateFilter => 10,
            _ => 1000
        };
    }
}
