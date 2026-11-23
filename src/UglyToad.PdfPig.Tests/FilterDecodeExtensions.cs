namespace UglyToad.PdfPig.Tests
{
    using PdfPig.Filters;
    using PdfPig.Tokens;

    internal static class FilterDecodeExtensions
    {
        /// <summary>
        /// Decodes with the default output limit, for tests that are not about the limit.
        /// </summary>
        public static Memory<byte> Decode(this IFilter filter, Memory<byte> input, DictionaryToken streamDictionary,
            IFilterProvider filterProvider, int filterIndex)
            => filter.Decode(input, streamDictionary, filterProvider, filterIndex, ParsingOptions.DefaultMaxDecompressedStreamBytes);
    }
}
