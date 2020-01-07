namespace UglyToad.PdfPig.Tests
{
    using System.Collections.Generic;
    using PdfPig.Filters;
    using PdfPig.Tokens;

    internal class TestFilterProvider : IFilterProvider
    {
        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary)
        {
            return new List<IFilter>();
        }

        public IReadOnlyList<IFilter> GetNamedFilters(IReadOnlyList<NameToken> names)
        {
            return new List<IFilter>();
        }

        public IReadOnlyList<IFilter> GetAllFilters()
        {
            return new List<IFilter>();
        }
    }
}