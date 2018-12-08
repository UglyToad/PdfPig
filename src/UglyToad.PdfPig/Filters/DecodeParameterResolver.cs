namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Tokens;

    internal class DecodeParameterResolver : IDecodeParameterResolver
    {
        private readonly ILog log;

        public DecodeParameterResolver(ILog log)
        {
            this.log = log;
        }

        public DictionaryToken GetFilterParameters(DictionaryToken streamDictionary, int index)
        {
            if (streamDictionary == null)
            {
                throw new ArgumentNullException(nameof(streamDictionary));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be 0 or greater");
            }

            var filter = GetDictionaryObject(streamDictionary, NameToken.Filter, NameToken.F);

            var parameters = GetDictionaryObject(streamDictionary, NameToken.DecodeParms, NameToken.Dp);

            switch (filter)
            {
                case NameToken _:
                    if (parameters is DictionaryToken dict)
                    {
                        return dict;
                    }
                    break;
                case ArrayToken array:
                    if (parameters is ArrayToken arr)
                    {
                        if (index < arr.Data.Count && array.Data[index] is DictionaryToken dictionary)
                        {
                            return dictionary;
                        }
                    }
                    break;
                default:
                    if (parameters != null)
                    {
                        log?.Error("Expected the decode parameters for the stream to be either an array or dictionary");
                    }
                    break;
            }

            return new DictionaryToken(new Dictionary<NameToken, IToken>());
        }

        private static IToken GetDictionaryObject(DictionaryToken dictionary, NameToken first, NameToken second)
        {
            if (dictionary.TryGet(first, out var token))
            {
                return token;
            }

            if (dictionary.TryGet(second, out token))
            {
                return token;
            }

            return null;
        }
    }
}