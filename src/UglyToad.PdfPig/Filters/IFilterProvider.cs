namespace UglyToad.PdfPig.Filters
{
    using System.Collections.Generic;
    using ContentStream;

    internal interface IFilterProvider
    {
        IReadOnlyList<IFilter> GetFilters(PdfDictionary streamDictionary);

        IReadOnlyList<IFilter> GetAllFilters();
    }
}