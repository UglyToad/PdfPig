namespace UglyToad.PdfPig.Tokens
{
    using System;
    using Core;

    /// <summary>
    /// Represents a string contained in a PDF document.
    /// <para>
    /// A string is a sequence of bytes, and what those bytes mean is decided by whoever reads them.
    /// The operand of a text showing operator is a sequence of character codes for the current font,
    /// read through <see cref="Bytes"/>. An entry the specification types as a <i>text string</i> is
    /// text, read through <see cref="Data"/>, which applies the rules in 7.9.2.2.
    /// </para>
    /// </summary>
    public sealed class StringToken : IDataToken<string>
    {
        private readonly byte[] rawBytes;

        private string? data;

        /// <summary>
        /// The string read as a text string (7.9.2.2): UTF-8 or UTF-16 where a byte order mark says
        /// so, PdfDocEncoding otherwise. Decoded on first use.
        /// </summary>
        public string Data => data ??= DecodeText();

        /// <summary>
        /// The bytes of the string, which for a token read from a file are the bytes it was read
        /// from.
        /// </summary>
        public ReadOnlySpan<byte> Bytes => rawBytes;

        /// <summary>
        /// The bytes of the string as memory. See <see cref="Bytes"/>.
        /// </summary>
        public ReadOnlyMemory<byte> Memory => rawBytes;

        /// <summary>
        /// Create a new <see cref="StringToken"/> from the bytes of a string in a PDF file.
        /// </summary>
        /// <param name="bytes">The bytes of the string.</param>
        public StringToken(byte[] bytes)
        {
            rawBytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        /// <summary>
        /// Create a new <see cref="StringToken"/> holding the given text, encoded as PdfDocEncoding
        /// where every character has a byte in it, and as UTF-16 with a byte order mark where they
        /// do not, or where the PdfDocEncoded bytes would themselves open with a byte order mark.
        /// </summary>
        /// <param name="data">The string data for the token to contain.</param>
        public StringToken(string data)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));

            // Text whose PdfDocEncoded bytes would open with a byte order mark cannot be stored that
            // way: reading them back takes the mark at face value and decodes the rest as UTF-8 or
            // UTF-16. Such text goes out as UTF-16 instead, which round-trips.
            if (PdfDocEncoding.TryConvertStringToBytes(data, out var pdfDocEncoded)
                && !StartsWithByteOrderMark(pdfDocEncoded!))
            {
                rawBytes = pdfDocEncoded!;
                return;
            }

            var utf16 = System.Text.Encoding.BigEndianUnicode.GetBytes(data);

            rawBytes = new byte[utf16.Length + 2];
            rawBytes[0] = 0xFE;
            rawBytes[1] = 0xFF;

            Array.Copy(utf16, 0, rawBytes, 2, utf16.Length);
        }

        /// <summary>
        /// The bytes of the string. See <see cref="Bytes"/>.
        /// </summary>
        public byte[] GetBytes()
        {
            return rawBytes;
        }

        private string DecodeText()
        {
            // PDF 2.0 added UTF-8, marked by a byte order mark, as a text string encoding.
            if (HasUtf8ByteOrderMark(rawBytes))
            {
                return System.Text.Encoding.UTF8.GetString(rawBytes, 3, rawBytes.Length - 3);
            }

            if (HasUtf16BigEndianByteOrderMark(rawBytes))
            {
                return System.Text.Encoding.BigEndianUnicode.GetString(rawBytes, 2, rawBytes.Length - 2);
            }

            // Not a text string encoding the specification defines, but it is accepted on the way in.
            if (HasUtf16LittleEndianByteOrderMark(rawBytes))
            {
                return System.Text.Encoding.Unicode.GetString(rawBytes, 2, rawBytes.Length - 2);
            }

            return PdfDocEncoding.TryConvertBytesToString(rawBytes, out var result)
                ? result!
                : OtherEncodings.BytesAsLatin1String(rawBytes);
        }

        private static bool HasUtf8ByteOrderMark(byte[] bytes)
            => bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

        private static bool HasUtf16BigEndianByteOrderMark(byte[] bytes)
            => bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF;

        private static bool HasUtf16LittleEndianByteOrderMark(byte[] bytes)
            => bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE;

        private static bool StartsWithByteOrderMark(byte[] bytes)
            => HasUtf8ByteOrderMark(bytes)
               || HasUtf16BigEndianByteOrderMark(bytes)
               || HasUtf16LittleEndianByteOrderMark(bytes);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
#if NET6_0_OR_GREATER
            hash.AddBytes(rawBytes.AsSpan());
#else
            var span = rawBytes.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                hash.Add(span[i]);
            }
#endif
            return hash.ToHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is IToken token && Equals(token);
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is StringToken other))
            {
                return false;
            }

            return Bytes.SequenceEqual(other.Bytes);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({Data})";
        }

    }
}
