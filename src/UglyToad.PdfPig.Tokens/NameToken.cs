namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using TextEncoding = System.Text.Encoding;
    using Core;

    /// <summary>
    /// A PDF name uniquely identified by its original sequence of bytes.
    /// </summary>
    public sealed partial class NameToken : IDataToken<string>
    {
        private readonly byte[] bytes;
        private readonly int hashCode;
        private string? data;
        private string? byteKey;

        /// <summary>
        /// The name decoded as UTF-8, or Latin-1 if the bytes are not valid UTF-8.
        /// This readable representation must not be used as a name identity or dictionary key.
        /// </summary>
        public string Data
        {
            get
            {
                if (data is not null) return data;
                data = IsValidUtf8(bytes) ? TextEncoding.UTF8.GetString(bytes) : ByteKey;
                if (IsAscii(bytes)) byteKey = data;
                return data;
            }
        }

        /// <summary>
        /// The name bytes after expansion of PDF #XX escapes.
        /// </summary>
        public ReadOnlySpan<byte> Bytes => bytes;

        // Only legacy string-dictionary callers need a byte-mapped string. ASCII
        // names share this string with Data instead of retaining two copies.
        internal string ByteKey
        {
            get
            {
                if (byteKey is not null) return byteKey;
                byteKey = OtherEncodings.BytesAsLatin1String(bytes);
                if (IsAscii(bytes)) data ??= byteKey;
                return byteKey;
            }
        }

        /// <summary>
        /// Returns a copy of the name bytes after expansion of PDF #XX escapes.
        /// </summary>
        public byte[] GetBytes() => (byte[])bytes.Clone();

        private NameToken(string text)
        {
            bytes = TextEncoding.UTF8.GetBytes(text);
            data = text;
            hashCode = ComputeHash(bytes);
            NameMap[bytes] = this;
        }

        private NameToken(byte[] ownedBytes)
        {
            bytes = ownedBytes;
            hashCode = ComputeHash(bytes);
        }

        /// <summary>
        /// Creates an interned name from text encoded as UTF-8.
        /// </summary>
        /// <param name="name">The readable name, without a leading slash.</param>
        /// <returns>The existing or new name token.</returns>
        public static NameToken Create(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
#if NET9_0_OR_GREATER
            var length = TextEncoding.UTF8.GetByteCount(name);
            if (length <= 256)
            {
                Span<byte> buffer = stackalloc byte[length];
                TextEncoding.UTF8.GetBytes(name.AsSpan(), buffer);
                return Create(buffer);
            }
#endif
            return CreateOwned(TextEncoding.UTF8.GetBytes(name));
        }

        /// <summary>
        /// Creates an interned name from its exact bytes after expansion of PDF #XX escapes.
        /// </summary>
        /// <param name="bytes">The name bytes, without a leading slash. The input is copied.</param>
        /// <returns>The existing or new byte-identical name token.</returns>
        public static NameToken Create(ReadOnlySpan<byte> bytes)
        {
#if NET9_0_OR_GREATER
            // The framework can compare borrowed bytes directly with stored names.
            if (NameLookup.TryGetValue(bytes, out var existing)) return existing;
#endif
            return CreateOwned(bytes.ToArray());
        }

        private static NameToken CreateOwned(byte[] ownedBytes)
        {
            var candidate = new NameToken(ownedBytes);
            return NameMap.GetOrAdd(ownedBytes, candidate);
        }


        // Legacy dictionary keys map characters directly to bytes, not UTF-8.
        internal static NameToken CreateFromByteKey(string key)
        {
#if NET9_0_OR_GREATER
            if (key.Length <= 256)
            {
                Span<byte> buffer = stackalloc byte[key.Length];
                for (var i = 0; i < key.Length; i++)
                {
                    // Keep the existing Latin-1 encoder fallback for keys which
                    // callers supplied outside the reversible byte-key range.
                    if (key[i] > 255)
                        return Create(OtherEncodings.StringAsLatin1Bytes(key).AsSpan());
                    buffer[i] = (byte)key[i];
                }
                return Create(buffer);
            }
#endif
            return CreateOwned(OtherEncodings.StringAsLatin1Bytes(key)!);
        }

        private static int ComputeHash(ReadOnlySpan<byte> bytes)
        {
            // HashCode is process-randomized. Hash the bytes directly; full byte
            // comparison in ByteComparer resolves collisions, including padded tails.
            var hash = new HashCode();
#if NET8_0_OR_GREATER
            hash.AddBytes(bytes);
#else
            var i = 0;
            for (; i <= bytes.Length - 4; i += 4)
                hash.Add(BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(i, 4)));
            for (; i < bytes.Length; i++) hash.Add(bytes[i]);
#endif
            return hash.ToHashCode();
        }

        private static bool IsAscii(ReadOnlySpan<byte> bytes)
        {
            foreach (var value in bytes) if (value > 127) return false;
            return true;
        }

#if !NET8_0_OR_GREATER
        private static readonly System.Text.Encoding StrictUtf8 = new System.Text.UTF8Encoding(false, true);
#endif

        private static bool IsValidUtf8(byte[] bytes)
        {
#if NET8_0_OR_GREATER
            return System.Text.Unicode.Utf8.IsValid(bytes);
#else
            // Older targets favor the standard decoder over a custom validator.
            // Invalid UTF-8 can take the slower exception path on these targets.
            try
            {
                StrictUtf8.GetCharCount(bytes);
                return true;
            }
            catch (System.Text.DecoderFallbackException)
            {
                return false;
            }
#endif
        }

        // Keys share the token's immutable byte array. Dictionary equality is
        // based on byte content rather than the default array reference identity.
        private sealed class ByteComparer : IEqualityComparer<byte[]>
#if NET9_0_OR_GREATER
            , IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>
#endif
        {
            public bool Equals(byte[]? x, byte[]? y) => ReferenceEquals(x, y)
                || x is not null && y is not null && x.AsSpan().SequenceEqual(y);
            public int GetHashCode(byte[] bytes) => ComputeHash(bytes);

#if NET9_0_OR_GREATER
            public bool Equals(ReadOnlySpan<byte> bytes, byte[] key) => bytes.SequenceEqual(key);
            public int GetHashCode(ReadOnlySpan<byte> bytes) => ComputeHash(bytes);
            public byte[] Create(ReadOnlySpan<byte> bytes) => bytes.ToArray();
#endif
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is NameToken token && Equals(token);

        /// <inheritdoc />
        public bool Equals(IToken obj) => obj is NameToken token && Equals(token);

        /// <summary>
        /// Are these names identical?
        /// </summary>
        public bool Equals(NameToken other) => ReferenceEquals(this, other)
            || other is not null && hashCode == other.hashCode && bytes.AsSpan().SequenceEqual(other.bytes);

        /// <inheritdoc />
        public override int GetHashCode() => hashCode;

        /// <summary>
        /// Convert the name token to a string implicitly.
        /// </summary>
        public static implicit operator string(NameToken name) => name?.Data;

        /// <summary>
        /// Checks if two names are equal.
        /// </summary>
        public static bool operator ==(NameToken name1, NameToken name2)
            => ReferenceEquals(name1, name2) || name1?.Equals(name2) == true;

        /// <summary>
        /// Checks two names for lack of equality.
        /// </summary>
        public static bool operator !=(NameToken name1, NameToken name2) => !(name1 == name2);

        /// <inheritdoc />
        public override string ToString() => $"/{Data}";
    }
}
