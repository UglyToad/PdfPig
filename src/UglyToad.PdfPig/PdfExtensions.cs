namespace UglyToad.PdfPig
{
    using System;
    using System.Diagnostics.CodeAnalysis;
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
        public static bool TryGet<T>(this DictionaryToken dictionary, NameToken name, IPdfTokenScanner tokenScanner, [NotNullWhen(true)] out T? token)
            where T : class, IToken
        {
            token = default;
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

        /// <summary>
        /// Get the entry with a given name and type or look-up the object if it's an indirect reference.
        /// </summary>
        public static T Get<T>(this DictionaryToken dictionary, NameToken name, IPdfTokenScanner scanner) where T : class, IToken
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
        public static Memory<byte> Decode(this StreamToken stream, IFilterProvider filterProvider)
        {
            var filters = filterProvider.GetFilters(stream.StreamDictionary);

            var transform = stream.Data;
            for (var i = 0; i < filters.Count; i++)
            {
                transform = filters[i].Decode(transform, stream.StreamDictionary, filterProvider, i);
            }

            return transform;
        }

        /// <summary>
        /// Get the decoded data from this stream.
        /// </summary>
        public static Memory<byte> Decode(this StreamToken stream, ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            var filters = filterProvider.GetFilters(stream.StreamDictionary, scanner);

            var transform = stream.Data;
            for (var i = 0; i < filters.Count; i++)
            {
                transform = filters[i].Decode(transform, stream.StreamDictionary, filterProvider, i);
            }

            return transform;
        }

        /// <summary>
        /// Returns an equivalent token where any indirect references of child objects are
        /// recursively traversed and resolved.
        /// </summary>
        internal static T? Resolve<T>(this T? token, IPdfTokenScanner scanner) where T : IToken
        {
            return (T?)ResolveInternal(token, scanner);
        }

        private static IToken? ResolveInternal(this IToken? token, IPdfTokenScanner scanner)
        {
            if (token is StreamToken stream)
            {
                return new StreamToken(Resolve(stream.StreamDictionary, scanner), stream.Data);
            }

            if (token is DictionaryToken dict)
            {
                var resolvedItems = new Dictionary<NameToken, IToken>();
                foreach (var kvp in dict.Data)
                {
                    var value = kvp.Value is IndirectReferenceToken reference ? scanner.Get(reference.Data)?.Data : kvp.Value;
                    resolvedItems[NameToken.Create(kvp.Key)] = ResolveInternal(value, scanner);
                }

                return new DictionaryToken(resolvedItems);
            }

            if (token is ArrayToken arr)
            {
                var resolvedItems = new List<IToken>();
                for (int i = 0; i < arr.Length; i++)
                {
                    var value = arr.Data[i] is IndirectReferenceToken reference ? scanner.Get(reference.Data)?.Data : arr.Data[i];
                    resolvedItems.Add(ResolveInternal(value, scanner));
                }
                return new ArrayToken(resolvedItems);
            }

            return token is IndirectReferenceToken tokenReference ? scanner.Get(tokenReference.Data)?.Data : token;
        }
    }
}
