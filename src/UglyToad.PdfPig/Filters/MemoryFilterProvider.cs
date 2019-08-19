namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using Exceptions;
    using Logging;
    using Tokens;
    using Util;

    internal class MemoryFilterProvider : IFilterProvider
    {
        private readonly IReadOnlyDictionary<string, IFilter> filterInstances; 

        public MemoryFilterProvider(IDecodeParameterResolver decodeParameterResolver, IPngPredictor pngPredictor, ILog log)
        {
            var ascii85 = new Ascii85Filter();
            var asciiHex = new AsciiHexDecodeFilter();
            var flate = new FlateFilter(decodeParameterResolver, pngPredictor, log);
            var runLength = new RunLengthFilter();
            var lzw = new LzwFilter(decodeParameterResolver, pngPredictor);

            filterInstances = new Dictionary<string, IFilter>
            {
                {NameToken.Ascii85Decode.Data, ascii85},
                {NameToken.Ascii85DecodeAbbreviation.Data, ascii85},
                {NameToken.AsciiHexDecode.Data, asciiHex},
                {NameToken.AsciiHexDecodeAbbreviation.Data, asciiHex},
                {NameToken.FlateDecode.Data, flate},
                {NameToken.FlateDecodeAbbreviation.Data, flate},
                {NameToken.RunLengthDecode.Data, runLength},
                {NameToken.RunLengthDecodeAbbreviation.Data, runLength},
                {NameToken.LzwDecode, lzw},
                {NameToken.LzwDecodeAbbreviation, lzw}
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
                return EmptyArray<IFilter>.Instance;
            }

            switch (token)
            {
                case ArrayToken filters:
                    var result = new IFilter[filters.Data.Count];
                    for (var i = 0; i < filters.Data.Count; i++)
                    {
                        var filterToken = filters.Data[i];
                        var filterName = ((NameToken) filterToken).Data;
                        result[i] = GetFilterStrict(filterName);
                    }

                    return result;
                case NameToken name:
                    return new[] { GetFilterStrict(name.Data) };
                default:
                    throw new PdfDocumentFormatException($"The filter for the stream was not a valid object. Expected name or array, instead got: {token}.");
            }
        }
        
        private IFilter GetFilterStrict(string name)
        {
            if (!filterInstances.TryGetValue(name, out var factory))
            {
                throw new NotSupportedException($"The filter with the name {name} is not supported yet. Please raise an issue.");
            }

            return factory;
        }

        public IReadOnlyList<IFilter> GetAllFilters()
        {
            throw new System.NotImplementedException();
        }
    }
}