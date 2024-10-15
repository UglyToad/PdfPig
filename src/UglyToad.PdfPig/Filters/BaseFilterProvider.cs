namespace UglyToad.PdfPig.Filters
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;
    using Util;

    /// <summary>
    /// Base abstract class for FilterProvider.
    /// </summary>
    public abstract class BaseFilterProvider : IFilterProvider
    {
        /// <summary>
        /// Dictionary of filters.
        /// </summary>
        protected readonly IReadOnlyDictionary<string, IFilter> FilterInstances;

        /// <summary>
        /// Create a new <see cref="BaseFilterProvider"/> with the given filters.
        /// </summary>
        /// <param name="filterInstances"></param>
        protected BaseFilterProvider(IReadOnlyDictionary<string, IFilter> filterInstances)
        {
            FilterInstances = filterInstances;
        }

        /// <inheritdoc />
        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary)
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
                    var result = new IFilter[filters.Data.Count];
                    for (var i = 0; i < filters.Data.Count; i++)
                    {
                        var filterToken = filters.Data[i];
                        var filterName = ((NameToken)filterToken).Data;
                        result[i] = GetFilterStrict(filterName);
                    }

                    return result;
                case NameToken name:
                    return new[] { GetFilterStrict(name.Data) };
                default:
                    throw new PdfDocumentFormatException($"The filter for the stream was not a valid object. Expected name or array, instead got: {token}.");
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<IFilter> GetNamedFilters(IReadOnlyList<NameToken> names)
        {
            if (names is null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            var result = new List<IFilter>();

            foreach (var name in names)
            {
                result.Add(GetFilterStrict(name));
            }

            return result;
        }

        private IFilter GetFilterStrict(string name)
        {
            if (!FilterInstances.TryGetValue(name, out var factory))
            {
                throw new NotSupportedException($"The filter with the name {name} is not supported yet. Please raise an issue.");
            }

            return factory;
        }

        /// <inheritdoc />
        public IReadOnlyList<IFilter> GetAllFilters()
        {
            return FilterInstances.Values.Distinct().ToList();
        }
    }
}
