namespace UglyToad.PdfPig.Filters
{
    using System.Collections.Generic;
    using ContentStream;
    using Tokenization.Tokens;

    internal interface IFilterProvider
    {
        IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary);

        IReadOnlyList<IFilter> GetFilters(PdfDictionary streamDictionary);

        IReadOnlyList<IFilter> GetAllFilters();
    }
}