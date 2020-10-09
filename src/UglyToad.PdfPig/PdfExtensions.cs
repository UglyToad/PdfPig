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
        public static bool TryGet<T>(this DictionaryToken dictionary, NameToken name, IPdfTokenScanner tokenScanner, out T token) where T : IToken
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
    }
}
