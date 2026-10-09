namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Util;

    /// <summary>Resolves PDF filter names, including indirect objects, through a document-scoped provider.</summary>
    /// <remarks>
    /// The wrapped provider supplies the filter instances. For a built-in CCITT filter with a
    /// different parsing mode, copy the returned list and substitute this document's configured
    /// instance. Keep shared providers and all other filter instances unchanged. This PdfPig
    /// integration applies ParsingOptions.UseLenientParsing without mutable global filter state;
    /// it does not implement a decompression algorithm.
    /// </remarks>
    internal class FilterProviderWithLookup : ILookupFilterProvider
    {
        private readonly IFilterProvider inner;
        private readonly CcittFaxDecodeFilter ccittFilter;

        public FilterProviderWithLookup(IFilterProvider inner, bool useLenientParsing = true)
        {
            this.inner = inner;
            ccittFilter = new CcittFaxDecodeFilter(useLenientParsing);
        }

        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary)
            => ConfigureFilters(inner.GetFilters(dictionary));

        public IReadOnlyList<IFilter> GetNamedFilters(IReadOnlyList<NameToken> names)
            => ConfigureFilters(inner.GetNamedFilters(names));

        public IReadOnlyList<IFilter> GetAllFilters()
            => ConfigureFilters(inner.GetAllFilters());

        private IReadOnlyList<IFilter> ConfigureFilters(IReadOnlyList<IFilter> filters)
        {
            IFilter[]? configuredFilters = null;
            for (var filterIndex = 0; filterIndex < filters.Count; filterIndex++)
            {
                if (filters[filterIndex] is CcittFaxDecodeFilter filter && filter.UseLenientParsing != ccittFilter.UseLenientParsing)
                {
                    // Replace only a built-in CCITT filter whose parsing mode differs. Copy
                    // the list so the document's policy does not alter a shared provider or
                    // caller-owned filter. Keep all other filter instances unchanged.
                    if (configuredFilters is null)
                    {
                        configuredFilters = filters.ToArray();
                    }
                    configuredFilters[filterIndex] = ccittFilter;
                }
            }
            return configuredFilters is null ? filters : configuredFilters;
        }

        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary, IPdfTokenScanner scanner)
        {
            if (dictionary is null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var token = dictionary.GetObjectOrDefault(NameToken.Filter, NameToken.F);
            if (token is null)
            {
                return Array.Empty<IFilter>();
            }

            switch (token)
            {
                case ArrayToken filters:
                    var result = new NameToken[filters.Data.Count];
                    for (var i = 0; i < filters.Data.Count; i++)
                    {
                        var filterToken = filters.Data[i];
                        var filterName = (NameToken)filterToken;
                        result[i] = filterName;
                    }

                    return GetNamedFilters(result);
                case NameToken name:
                    return GetNamedFilters(new[] {name});
                case IndirectReferenceToken irt:
                    if (DirectObjectFinder.TryGet<NameToken>(irt, scanner, out var indirectName))
                    {
                        return GetNamedFilters(new[] { indirectName });
                    }
                    else if (DirectObjectFinder.TryGet<ArrayToken>(irt, scanner, out var indirectArray))
                    {
                        return GetNamedFilters(indirectArray.Data.Select(x => (NameToken) x).ToList());
                    }
                    else
                    {
                        throw new PdfDocumentFormatException($"The filter for the stream was not a valid object. Expected name or array, instead got: {token}.");
                    }
                default:
                    throw new PdfDocumentFormatException($"The filter for the stream was not a valid object. Expected name or array, instead got: {token}.");
            }
        }
    }
}