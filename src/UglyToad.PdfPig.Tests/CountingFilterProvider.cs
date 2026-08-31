namespace UglyToad.PdfPig.Tests
{
    using PdfPig.Filters;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    /// <summary>
    /// A filter provider whose single pass-through filter counts how many times it ran, so a test can
    /// distinguish "the bytes came back" from "the bytes were decoded again to come back".
    /// </summary>
    internal sealed class CountingFilterProvider : ILookupFilterProvider
    {
        private readonly IReadOnlyList<IFilter> filters;

        /// <summary>
        /// The number of times a stream has actually been run through the filter pipeline.
        /// </summary>
        public int DecodeCount { get; private set; }

        public CountingFilterProvider(bool throwOnDecode = false)
        {
            filters = new IFilter[] { new CountingFilter(this, throwOnDecode) };
        }

        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary) => filters;

        public IReadOnlyList<IFilter> GetNamedFilters(IReadOnlyList<NameToken> names) => filters;

        public IReadOnlyList<IFilter> GetAllFilters() => filters;

        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary, IPdfTokenScanner scanner) => filters;

        private sealed class CountingFilter : IFilter
        {
            private readonly CountingFilterProvider owner;
            private readonly bool throwOnDecode;

            public CountingFilter(CountingFilterProvider owner, bool throwOnDecode)
            {
                this.owner = owner;
                this.throwOnDecode = throwOnDecode;
            }

            public bool IsSupported => true;

            public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary,
                IFilterProvider filterProvider, int filterIndex)
            {
                owner.DecodeCount++;

                if (throwOnDecode)
                {
                    throw new InvalidOperationException("Corrupt stream.");
                }

                return input;
            }
        }
    }
}
