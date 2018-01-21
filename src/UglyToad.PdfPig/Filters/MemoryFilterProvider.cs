namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Exceptions;
    using Logging;
    using Tokenization.Tokens;

    internal class MemoryFilterProvider : IFilterProvider
    {
        private readonly IReadOnlyDictionary<string, Func<IFilter>> filterFactories; 

        public MemoryFilterProvider(IDecodeParameterResolver decodeParameterResolver, IPngPredictor pngPredictor, ILog log)
        {
            IFilter Ascii85Func() => new Ascii85Filter();
            IFilter AsciiHexFunc() => new AsciiHexDecodeFilter();
            IFilter FlateFunc() => new FlateFilter(decodeParameterResolver, pngPredictor, log);
            IFilter RunLengthFunc() => new RunLengthFilter();

            filterFactories = new Dictionary<string, Func<IFilter>>
            {
                {NameToken.Ascii85Decode.Data, Ascii85Func},
                {NameToken.Ascii85DecodeAbbreviation.Data, Ascii85Func},
                {NameToken.AsciiHexDecode.Data, AsciiHexFunc},
                {NameToken.AsciiHexDecodeAbbreviation.Data, AsciiHexFunc},
                {NameToken.FlateDecode.Data, FlateFunc},
                {NameToken.FlateDecodeAbbreviation.Data, FlateFunc},
                {NameToken.RunLengthDecode.Data, RunLengthFunc},
                {NameToken.RunLengthDecodeAbbreviation.Data, RunLengthFunc}
            };
        }

        public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.TryGet(NameToken.Filter, out var token))
            {
                return new IFilter[0];
            }

            switch (token)
            {
                case ArrayToken filters:
                    // TODO: presumably this may be invalid...
                    return filters.Data.Select(x => GetFilterStrict(((NameToken)x).Data)).ToList();
                case NameToken name:
                    return new[] { GetFilterStrict(name.Data) };
                default:
                    throw new PdfDocumentFormatException($"The filter for the stream was not a valid object. Expected name or array, instead got: {token}.");
            }
        }
        
        private IFilter GetFilterStrict(string name)
        {
            if (!filterFactories.TryGetValue(name, out var factory))
            {
                throw new NotSupportedException($"The filter with the name {name} is not supported yet. Please raise an issue.");
            }

            return factory();
        }

        public IReadOnlyList<IFilter> GetAllFilters()
        {
            throw new System.NotImplementedException();
        }
    }
}