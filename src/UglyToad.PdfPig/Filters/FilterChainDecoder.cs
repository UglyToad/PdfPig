namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Tokens;

    /// <summary>
    /// Decodes a stream through its filters, giving each the most it may decode to: the document's limit,
    /// and for all but the last filter the estimate of how far the filters so far can plausibly expand.
    /// </summary>
    internal static class FilterChainDecoder
    {
        public static Memory<byte> Decode(Memory<byte> input, DictionaryToken dictionary,
            IFilterProvider provider, IReadOnlyList<IFilter> filters)
            => Decode(input, dictionary, provider, filters, GetOutputLimit(provider));

        public static Memory<byte> Decode(Memory<byte> input, DictionaryToken dictionary,
            IFilterProvider provider, IReadOnlyList<IFilter> filters, int maximumOutputLength)
        {
            double estimatedLimit = 100d * input.Length;
            var decoded = input;
            for (var i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                estimatedLimit *= GetEstimatedSizeMultiplier(filter);
                // Preserve the existing ratio policy: it applies only to intermediate stages.
                var checkRatio = i < filters.Count - 1 && estimatedLimit < maximumOutputLength;
                var outputLimit = checkRatio ? (int)estimatedLimit : maximumOutputLength;
                try
                {
                    decoded = filter.Decode(decoded, dictionary, provider, i, outputLimit);
                }
                catch (DecodeBuffer.LimitExceededException ex) when (checkRatio)
                {
                    // Retain the public exception type and message prefix of the old ratio guard.
                    throw new PdfDocumentFormatException(
                        $"Decoded stream size exceeds the estimated maximum size. Output limit: {outputLimit}, {i + 1} filters applied out of {filters.Count}.", ex);
                }

                // A filter that does not enforce the limit itself is held to it once it returns.
                if (decoded.Length > outputLimit)
                {
                    if (checkRatio)
                    {
                        throw new PdfDocumentFormatException(
                            $"Decoded stream size exceeds the estimated maximum size. Current decoded stream length: {decoded.Length}, {i + 1} filters applied out of {filters.Count}.");
                    }

                    throw new DecodeBuffer.LimitExceededException();
                }
            }
            return decoded;
        }

        /// <summary>The limit of the document the provider belongs to, or the default for a provider on its own.</summary>
        private static int GetOutputLimit(IFilterProvider provider)
            => provider is ILookupFilterProvider lookup ? lookup.MaxDecompressedStreamBytes : DecodeBuffer.MaximumCapacity;

        private static double GetEstimatedSizeMultiplier(IFilter filter) => filter switch
        {
            AsciiHexDecodeFilter => 0.5,
            Ascii85Filter => 0.8,
            RunLengthFilter => 1.5,
            LzwFilter => 2,
            FlateFilter => 10,
            CryptFilter => 1,
            _ => 1000
        };
    }
}
