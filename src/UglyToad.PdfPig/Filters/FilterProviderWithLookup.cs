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

    internal class FilterProviderWithLookup : ILookupFilterProvider
    {
        private readonly IFilterProvider inner;

        public int MaxDecompressedStreamBytes { get; }

        public FilterProviderWithLookup(IFilterProvider inner, int maxDecompressedStreamBytes = ParsingOptions.DefaultMaxDecompressedStreamBytes)
        {
            if (maxDecompressedStreamBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDecompressedStreamBytes), maxDecompressedStreamBytes,
                    "The maximum decompressed stream size must be positive.");
            }

            this.inner = inner;
            MaxDecompressedStreamBytes = maxDecompressedStreamBytes;
        }

        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary)
            => inner.GetFilters(dictionary);

        public IReadOnlyList<IFilter> GetNamedFilters(IReadOnlyList<NameToken> names)
            => inner.GetNamedFilters(names);

        public IReadOnlyList<IFilter> GetAllFilters()
            => inner.GetAllFilters();

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