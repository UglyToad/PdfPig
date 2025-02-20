namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using Tokens;
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// Decode parameter resolver.
    /// </summary>
    public static class DecodeParameterResolver
    {
        /// <summary>
        /// Get the filter parameters from a stream dictionary.
        /// </summary>
        /// <param name="streamDictionary">The stream dictionary.</param>
        /// <param name="index">If the filter element is an <see cref="ArrayToken"/>, the index in the array to take the dictionary from.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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