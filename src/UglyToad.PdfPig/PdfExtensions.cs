namespace UglyToad.PdfPig
{
    using System.Collections.Generic;
    using Core;
    using Filters;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Extensions for PDF types.
    /// </summary>
    public static class PdfExtensions
    {
        /// <summary>
        /// Try and get the entry with a given name and type or look-up the object if it's an indirect reference.
        /// </summary>
        internal static bool TryGet<T>(this DictionaryToken dictionary, NameToken name, IPdfTokenScanner tokenScanner, out T token) where T : IToken
        {
            token = default(T);
            if (!dictionary.TryGet(name, out var t) || !(t is T typedToken))
            {
                if (t is IndirectReferenceToken reference)
                {
                    return DirectObjectFinder.TryGet(reference, tokenScanner, out token);
                }

                return false;
            }

            token = typedToken;
            return true;
        }

        internal static T Get<T>(this DictionaryToken dictionary, NameToken name, IPdfTokenScanner scanner) where T : class, IToken
        {
            if (!dictionary.TryGet(name, out var token) || !(token is T typedToken))
            {
                if (!(token is IndirectReferenceToken indirectReference))
                {
                    throw new PdfDocumentFormatException($"Dictionary does not contain token with name {name} of type {typeof(T).Name}.");
                }

                typedToken = DirectObjectFinder.Get<T>(indirectReference, scanner);
            }

            return typedToken;
        }

        internal static IToken GetDictionaryObject(this DictionaryToken dictionary, NameToken name)
        {
            if (dictionary.TryGet(name, out var token))
            {
                return token;
            }

            return null;
        }

        internal static IToken GetDictionaryObject(this DictionaryToken dictionary, NameToken first, NameToken second)
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

        internal static int GetInt(this DictionaryToken dictionary, NameToken name, int defaultValue)
        {
            var numericToken = dictionary.GetDictionaryObject(name) as NumericToken;
            return numericToken?.Int ?? defaultValue;
        }

        internal static int GetInt(this DictionaryToken dictionary, NameToken first, NameToken second, int defaultValue)
        {
            var numericToken = dictionary.GetDictionaryObject(first, second) as NumericToken;
            return numericToken?.Int ?? defaultValue;
        }

        internal static bool GetBoolean(this DictionaryToken dictionary, NameToken name, bool defaultValue)
        {
            var booleanToken = dictionary.GetDictionaryObject(name) as BooleanToken;
            return booleanToken?.Data ?? defaultValue;
        }

        /// <summary>
        /// Get the decoded data from this stream.
        /// </summary>
        public static IReadOnlyList<byte> Decode(this StreamToken stream, IFilterProvider filterProvider)
        {
            var filters = filterProvider.GetFilters(stream.StreamDictionary);

            var transform = stream.Data;
            for (var i = 0; i < filters.Count; i++)
            {
                transform = filters[i].Decode(transform, stream.StreamDictionary, i);
            }

            return transform;
        }

        internal static IReadOnlyList<byte> Decode(this StreamToken stream, ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            var filters = filterProvider.GetFilters(stream.StreamDictionary, scanner);

            var transform = stream.Data;
            for (var i = 0; i < filters.Count; i++)
            {
                transform = filters[i].Decode(transform, stream.StreamDictionary, i);
            }

            return transform;
        }
    }
}
