namespace UglyToad.PdfPig.Tokens
{
    using System;
    using Core;
    using System.Collections.ObjectModel;
    using System.Collections;
    using System.Threading;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A dictionary object is an associative table containing pairs of objects, known as the dictionary's entries. 
    /// The key must be a <see cref="NameToken"/> and the value may be an kind of <see cref="IToken"/>.
    /// </summary>
    public sealed class DictionaryToken : IDataToken<IReadOnlyDictionary<string, IToken>>, IEquatable<DictionaryToken>
    {
        private int hashCode;
        private bool hashCodeComputed;

        private IReadOnlyDictionary<string, IToken>? data;

        /// <summary>
        /// The entries keyed by a reversible Latin-1 mapping of each name's bytes.
        /// These keys are identities, not decoded text. Prefer name-token lookups.
        /// Reconstruct a key using NameToken.Create(OtherEncodings.StringAsLatin1Bytes(key)).
        /// </summary>
        public IReadOnlyDictionary<string, IToken> Data => data ??= new ReadOnlyDictionary<string, IToken>(
            Entries.ToDictionary(x => x.Key.ByteKey, x => x.Value, StringComparer.Ordinal));

        /// <summary>
        /// The entries keyed by their exact PDF name identities. Prefer this view to Data.
        /// </summary>
        public IReadOnlyDictionary<NameToken, IToken> Entries { get; }
        
        /// <summary>
        /// Empty DictionaryToken instance
        /// </summary>
        public static readonly DictionaryToken Empty = new(new Dictionary<string, IToken>());

        /// <summary>
        /// Create a new <see cref="DictionaryToken"/>.
        /// </summary>
        /// <param name="data">The data this dictionary will contain.</param>
        public DictionaryToken(IReadOnlyDictionary<NameToken, IToken> data)
            : this(data?.ToDictionary(x => x.Key, x => x.Value) ?? throw new ArgumentNullException(nameof(data)))
        {
        }

        // Internal edits already create a fresh dictionary. Take ownership here
        // instead of copying that private dictionary a second time.
        private DictionaryToken(Dictionary<NameToken, IToken> ownedEntries)
        {
            Entries = new ReadOnlyDictionary<NameToken, IToken>(ownedEntries);
        }
        private DictionaryToken(IReadOnlyDictionary<string, IToken> data)
        {
            // Preserve the live string-dictionary view used by existing writer callers.
            this.data = data;
            Entries = new ByteKeyDictionary(data);
        }

        private sealed class ByteKeyDictionary : IReadOnlyDictionary<NameToken, IToken>
        {
            private readonly IReadOnlyDictionary<string, IToken> source;

            public ByteKeyDictionary(IReadOnlyDictionary<string, IToken> source) => this.source = source;
            public int Count => source.Count;
            public IEnumerable<NameToken> Keys => source.Keys.Select(ToName);
            public IEnumerable<IToken> Values => source.Values;
            public IToken this[NameToken key] => source[key.ByteKey];
            public bool ContainsKey(NameToken key) => source.ContainsKey(key.ByteKey);
            public bool TryGetValue(NameToken key, out IToken value) => source.TryGetValue(key.ByteKey, out value);

            private static NameToken ToName(string key) => NameToken.CreateFromByteKey(key);

            public IEnumerator<KeyValuePair<NameToken, IToken>> GetEnumerator()
                => source.Select(x => new KeyValuePair<NameToken, IToken>(ToName(x.Key), x.Value)).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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

            return Entries.TryGetValue(name, out token);
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
            return Entries.ContainsKey(name);
        }

        /// <summary>
        /// Create a copy of this dictionary with the additional entry (or override the value of the existing entry).
        /// </summary>
        /// <param name="key">The key of the entry to create or override.</param>
        /// <param name="value">The value of the entry to create or override.</param>
        /// <returns>A new <see cref="DictionaryToken"/> with the entry created or modified.</returns>
        public DictionaryToken With(NameToken key, IToken value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            var result = Entries.ToDictionary(x => x.Key, x => x.Value);
            result[key] = value;
            return new DictionaryToken(result);
        }

        /// <summary>
        /// Create or replace an entry using its reversible Latin-1 byte key from Data.
        /// </summary>
        public DictionaryToken With(string key, IToken value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return With(NameToken.CreateFromByteKey(key), value);
        }

        /// <summary>
        /// Creates a copy of this dictionary with the entry with the specified key removed (if it exists).
        /// </summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <returns>A new <see cref="DictionaryToken"/> with the entry removed.</returns>
        public DictionaryToken Without(NameToken key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return new DictionaryToken(Entries.Where(x => x.Key != key).ToDictionary(x => x.Key, x => x.Value));
        }

        /// <summary>
        /// Remove an entry using its reversible Latin-1 byte key from Data.
        /// </summary>
        public DictionaryToken Without(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Without(NameToken.CreateFromByteKey(key));
        }

        /// <summary>
        /// Create a new <see cref="DictionaryToken"/>.
        /// </summary>
        /// <param name="data">The reversible Latin-1 byte keys and values. The existing live view is retained.</param>
        public static DictionaryToken With(IReadOnlyDictionary<string, IToken> data)
        {
            return new DictionaryToken(data ?? throw new ArgumentNullException(nameof(data)));
        }

        private int ComputeHashCode()
        {
            // Equals is insensitive to entry order so the hash must be too
            int hash = 0;

            foreach (var kvp in Entries)
            {
                unchecked
                {
                    hash += HashCode.Combine(kvp.Key, kvp.Value);
                }
            }

            return hash;
        }
        
        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (!Volatile.Read(ref hashCodeComputed))
            {
                hashCode = ComputeHashCode();
                Volatile.Write(ref hashCodeComputed, true);
            }

            return hashCode;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is DictionaryToken token && Equals(token);
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            return obj is DictionaryToken token && Equals(token);
        }

        /// <inheritdoc />
        public bool Equals(DictionaryToken other)
        {
            if (other is null)
            { 
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Entries.Count != other.Entries.Count)
            {
                return false;
            }

            foreach (var kvp in other.Entries)
            {
                if (!Entries.TryGetValue(kvp.Key, out var val) || !val.Equals(kvp.Value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Join(", ", Entries.Select(x => $"<{x.Key.Data}, {x.Value}>"));
        }
    }
}
