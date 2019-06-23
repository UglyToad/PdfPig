namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Exceptions;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A dictionary object is an associative table containing pairs of objects, known as the dictionary's entries. 
    /// The key must be a <see cref="NameToken"/> and the value may be an kind of <see cref="IToken"/>.
    /// </summary>
    public class DictionaryToken : IDataToken<IReadOnlyDictionary<string, IToken>>
    {
        /// <summary>
        /// The key value pairs in this dictionary.
        /// </summary>
        [NotNull]
        public IReadOnlyDictionary<string, IToken> Data { get; }

        /// <summary>
        /// Create a new <see cref="DictionaryToken"/>.
        /// </summary>
        /// <param name="data">The data this dictionary will contain.</param>
        public DictionaryToken([NotNull]IReadOnlyDictionary<NameToken, IToken> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var result = new Dictionary<string, IToken>(data.Count);

            foreach (var keyValuePair in data)
            {
                result[keyValuePair.Key.Data] = keyValuePair.Value;
            }

            Data = result;
        }

        private DictionaryToken(IReadOnlyDictionary<string, IToken> data)
        {
            Data = data;
        }

        internal T Get<T>(NameToken name, IPdfTokenScanner scanner) where T : IToken
        {
            if (!TryGet(name, out var token) || !(token is T typedToken))
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
        /// Try and get the entry with a given name.
        /// </summary>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <param name="token">The token, if it is found.</param>
        /// <returns><see langword="true"/> if the token is found, <see langword="false"/> otherwise.</returns>
        public bool TryGet(NameToken name, out IToken token)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return Data.TryGetValue(name.Data, out token);
        }

        /// <summary>
        /// Try and get the entry with a given name and type or look-up the object if it's an indirect reference.
        /// </summary>
        internal bool TryGet<T>(NameToken name, IPdfTokenScanner tokenScanner, out T token) where T : IToken
        {
            token = default(T);
            if (!TryGet(name, out var t) || !(t is T typedToken))
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
        /// Try and get the entry with a given name and a specific data type.
        /// </summary>
        /// <typeparam name="T">The expected data type of the dictionary value.</typeparam>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <param name="token">The token, if it is found.</param>
        /// <returns><see langword="true"/> if the token is found with this type, <see langword="false"/> otherwise.</returns>
        public bool TryGet<T>(NameToken name, out T token) where T : IToken
        {
            token = default(T);
            if (!TryGet(name, out var t) || !(t is T typedToken))
            {
                return false;
            }

            token = typedToken;
            return true;
        }

        /// <summary>
        /// Whether the dictionary contains an entry with this name.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns><see langword="true"/> if the token is found, <see langword="false"/> otherwise.</returns>
        public bool ContainsKey(NameToken name)
        {
            return Data.ContainsKey(name.Data);
        }

        /// <summary>
        /// Create a copy of this dictionary with the additional entry (or override the value of the existing entry).
        /// </summary>
        /// <param name="key">The key of the entry to create or override.</param>
        /// <param name="value">The value of the entry to create or override.</param>
        /// <returns>A new <see cref="DictionaryToken"/> with the entry created or modified.</returns>
        public DictionaryToken With(NameToken key, IToken value) => With(key.Data, value);

        /// <summary>
        /// Create a copy of this dictionary with the additional entry (or override the value of the existing entry).
        /// </summary>
        /// <param name="key">The key of the entry to create or override.</param>
        /// <param name="value">The value of the entry to create or override.</param>
        /// <returns>A new <see cref="DictionaryToken"/> with the entry created or modified.</returns>
        public DictionaryToken With(string key, IToken value)
        {
            var result = new Dictionary<string, IToken>(Data.Count + 1);

            foreach (var keyValuePair in Data)
            {
                result[keyValuePair.Key] = keyValuePair.Value;
            }

            result[key] = value;

            return new DictionaryToken(result);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Join(", ", Data.Select(x => $"<{x.Key}, {x.Value}>"));
        }
    }
}
