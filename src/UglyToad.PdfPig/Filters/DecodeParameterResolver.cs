namespace UglyToad.PdfPig.Filters
{
    using Tokens;
    using Util;

    internal static class DecodeParameterResolver
    {
        public static DictionaryToken GetFilterParameters(DictionaryToken streamDictionary, int index)
        {
            if (streamDictionary is null)
            {
                throw new ArgumentNullException(nameof(streamDictionary));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be 0 or greater");
            }

            var filter = streamDictionary.GetObjectOrDefault(NameToken.Filter, NameToken.F);

            var parameters = streamDictionary.GetObjectOrDefault(NameToken.DecodeParms, NameToken.Dp);

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
                        if (index < arr.Data.Count && arr.Data[index] is DictionaryToken dictionary)
                        {
                            return dictionary;
                        }
                    }
                    break;
                default:
                    break;
            }

            return new DictionaryToken(new Dictionary<NameToken, IToken>());
        }
    }
}