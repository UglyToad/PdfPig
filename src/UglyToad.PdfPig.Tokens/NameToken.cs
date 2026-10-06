namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Text;
    using Core;

    /// <inheritdoc />
    /// <summary>
    /// A name object is an atomic symbol uniquely defined by a sequence of bytes.
    /// Each name is considered identical if it has the same sequence of bytes. Names are used in
    /// PDF documents to identify dictionary keys and other elements of a PDF document.
    /// </summary>
    public sealed partial class NameToken : IDataToken<string>
    {
        private static readonly System.Text.Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private readonly byte[] bytes;
        private readonly string identity;
        private string? data;

        /// <summary>
        /// The name decoded as UTF-8, or Latin-1 if the bytes are not valid UTF-8.
        /// This readable representation must not be used as a name identity or dictionary key.
        /// </summary>
        public string Data
        {
            get
            {
                if (data != null) return data;
                try
                {
                    return data = StrictUtf8.GetString(bytes);
                }
                catch (DecoderFallbackException)
                {
                    return data = identity;
                }
            }
        }

        /// <summary>
        /// The name bytes after expansion of PDF #XX escapes.
        /// </summary>
        public ReadOnlySpan<byte> Bytes => bytes;

        /// <summary>
        /// Returns a copy of the name bytes after expansion of PDF #XX escapes.
        /// </summary>
        public byte[] GetBytes() => (byte[])bytes.Clone();

        private NameToken(string text)
            : this(System.Text.Encoding.UTF8.GetBytes(text))
        {
            NameMap[identity] = this;
        }

        private NameToken(byte[] bytes)
            : this(bytes, OtherEncodings.BytesAsLatin1String(bytes))
        {
        }

        private NameToken(byte[] bytes, string identity)
        {
            this.bytes = bytes;
            this.identity = identity;
        }

        /// <summary>
        /// Creates an interned name from text encoded as UTF-8.
        /// </summary>
        /// <param name="name">The readable name, without a leading slash.</param>
        /// <returns>The existing or new name token.</returns>
        public static NameToken Create(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return Create(System.Text.Encoding.UTF8.GetBytes(name).AsSpan());
        }

        /// <summary>
        /// Creates an interned name from its exact bytes after expansion of PDF #XX escapes.
        /// </summary>
        /// <param name="bytes">The name bytes, without a leading slash. The input is copied.</param>
        /// <returns>The existing or new byte-identical name token.</returns>
        public static NameToken Create(ReadOnlySpan<byte> bytes)
        {
            var identity = OtherEncodings.BytesAsLatin1String(bytes);
            if (NameMap.TryGetValue(identity, out var value)) return value;
            return NameMap.GetOrAdd(identity, new NameToken(bytes.ToArray(), identity));
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is NameToken token && Equals(token);
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            return obj is NameToken token && Equals(token);
        }

        /// <summary>
        /// Are these names identical?
        /// </summary>
        public bool Equals(NameToken other)
        {
            return string.Equals(identity, other?.identity, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return identity.GetHashCode();
        }

        /// <summary>
        /// Convert the name token to a string implicitly.
        /// </summary>
        /// <param name="name">The name token to convert.</param>
        public static implicit operator string(NameToken name)
        {
            return name?.Data;
        }

        /// <summary>
        /// Checks if two names are equal.
        /// </summary>
        public static bool operator ==(NameToken name1, NameToken name2)
        {
            if (ReferenceEquals(name1, name2))
            {
                return true;
            }

            return name1?.Equals(name2) ?? false;
        }

        /// <summary>
        /// Checks two names for lack of equality.
        /// </summary>
        public static bool operator !=(NameToken name1, NameToken name2)
        {
            return !(name1 == name2);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Data}";
        }
    }
}