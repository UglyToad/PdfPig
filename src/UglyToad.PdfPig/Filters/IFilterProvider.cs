namespace UglyToad.PdfPig.Filters
{
    using System.Collections.Generic;
    using Tokens;

    internal interface IFilterProvider
    {
        IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary);

        IReadOnlyList<IFilter> GetAllFilters();
    }
}