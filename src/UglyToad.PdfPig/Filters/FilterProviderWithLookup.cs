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

    /// <summary>
    /// Gets filter implementations (<see cref="IFilter"/>) for decoding PDF data.
    /// </summary>
    public class FilterProviderWithLookup : ILookupFilterProvider
    {
        private readonly IFilterProvider inner;
        /// <summary>
        /// /// <summary>
        /// Gets filter implementations (<see cref="IFilter"/>) for decoding PDF data.
        /// </summary>
        /// </summary>
        /// <param name="inner"></param>
        public FilterProviderWithLookup(IFilterProvider inner)
        {
            this.inner = inner;
        }
        /// <summary>
        /// Get all available filters in this library.
        /// </summary>
        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary)
            => inner.GetFilters(dictionary);
        /// <summary>
        /// Gets the filters specified by the filter names.
        /// </summary>
        public IReadOnlyList<IFilter> GetNamedFilters(IReadOnlyList<NameToken> names)
            => inner.GetNamedFilters(names);
        /// <summary>
        /// Get all available filters in this library.
        /// </summary>
        public IReadOnlyList<IFilter> GetAllFilters()
            => inner.GetAllFilters();
        /// <summary>
        /// Get all available filters in this library.
        /// </summary>
        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary, IPdfTokenScanner scanner)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var token = dictionary.GetObjectOrDefault(NameToken.Filter, NameToken.F);
            if (token == null)
            {
                return EmptyArray<IFilter>.Instance;
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
                        return GetNamedFilters(new []{ indirectName });
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