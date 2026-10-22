namespace UglyToad.PdfPig
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Core;
    using Filters;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Util;

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

            return FilterChainDecoder.Decode(stream.Data, stream.StreamDictionary, filterProvider, filters);
        }

        /// <summary>
        /// Get the decoded data from this stream.
        /// </summary>
        public static Memory<byte> Decode(this StreamToken stream, ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            var filters = filterProvider.GetFilters(stream.StreamDictionary, scanner);
            return FilterChainDecoder.Decode(stream.Data, stream.StreamDictionary, filterProvider, filters);
        }

        /// <summary>
        /// Returns an equivalent token where any indirect references of child objects are
        /// recursively traversed and resolved.
        /// </summary>
        internal static T? Resolve<T>(this T? token, IPdfTokenScanner scanner, HashSet<IndirectReference>? visited = null) where T : IToken
        {
            return (T?)ResolveInternal(token, scanner, visited ?? []);
        }

        private static IToken? ResolveInternal(this IToken? token, IPdfTokenScanner scanner, HashSet<IndirectReference> visited)
        {
            if (token is StreamToken stream)
            {
                return new StreamToken(Resolve(stream.StreamDictionary, scanner, visited), stream.Data);
            }

            if (token is DictionaryToken dict)
            {
                var resolvedItems = new Dictionary<NameToken, IToken>();
                foreach (var kvp in dict.Data)
                {
                    var value = kvp.Value;
                    if (kvp.Value is IndirectReferenceToken reference)
                    {
                        if (visited.Contains(reference.Data))
                        {
                            continue;
                        }
                        value = scanner.Get(reference.Data)?.Data;
                        visited.Add(reference.Data);
                    }
                    resolvedItems[NameToken.Create(kvp.Key)] = ResolveInternal(value, scanner, visited);
                }

                if (resolvedItems.Count != dict.Data.Count)
                {
                    if (resolvedItems.Count > dict.Data.Count)
                    {
                        throw new InvalidOperationException("Resolved more items than were present in the original dictionary. This should not be possible.");
                    }

                    // We missed some due to cycles, try and resolve them now.
                    foreach (var missing in dict.Data.Keys.Except(resolvedItems.Keys.Select(k => k.Data), StringComparer.OrdinalIgnoreCase))
                    {
                        if (dict.Data[missing] is IndirectReferenceToken reference)
                        {
                            resolvedItems[NameToken.Create(missing)] = ResolveInternal(reference, scanner, visited);
                        }
                    }
                }

                return new DictionaryToken(resolvedItems);
            }

            if (token is ArrayToken arr)
            {
                var resolvedItems = new List<IToken>();
                for (int i = 0; i < arr.Length; i++)
                {
                    var value = arr.Data[i] is IndirectReferenceToken reference ? scanner.Get(reference.Data)?.Data : arr.Data[i];
                    resolvedItems.Add(ResolveInternal(value, scanner, visited));
                }
                return new ArrayToken(resolvedItems);
            }

            return token is IndirectReferenceToken tokenReference ? scanner.Get(tokenReference.Data)?.Data : token;
        }
    }
}
