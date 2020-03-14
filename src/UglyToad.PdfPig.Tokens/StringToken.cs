namespace UglyToad.PdfPig.Tokens
{
    using System;
    using Core;

    /// <summary>
    /// Represents a string of text contained in a PDF document.
    /// </summary>
    public class StringToken : IDataToken<string>
    {
        /// <summary>
        /// The string in the token.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// The encoding used to generate the <see langword="string"/> in <see cref="Data"/>
        /// from the bytes in the file.
        /// </summary>
        public Encoding EncodedWith { get; }

        /// <summary>
        /// Create a new <see cref="StringToken"/>.
        /// </summary>
        /// <param name="data">The string data for the token to contain.</param>
        /// <param name="encodedWith">The encoding used to generate the <see cref="Data"/>.</param>
        public StringToken(string data, Encoding encodedWith = Encoding.Iso88591)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            EncodedWith = encodedWith;
        }

        /// <summary>
        /// Convert the <see langword="string"/> in <see cref="Data"/> back to bytes.
        /// </summary>
        public byte[] GetBytes()
        {
            switch (EncodedWith)
            {
                case Encoding.Utf16BE:
                {
                    var data = System.Text.Encoding.BigEndianUnicode.GetBytes(Data);

                    var result = new byte[data.Length + 2];
                    result[0] = 0xFE;
                    result[1] = 0xFF;

                    Array.Copy(data, 0, result, 2, data.Length);

                    return result;
                }
                case Encoding.Utf16:
                {
                    return System.Text.Encoding.Unicode.GetBytes(Data);
                }
                default:
                    return OtherEncodings.StringAsLatin1Bytes(Data);
            }
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

            return EncodedWith.Equals(other.EncodedWith) && Data.Equals(other.Data);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({Data})";
        }

        /// <summary>
        /// The encoding used to convert the underlying file bytes to the string.
        /// </summary>
        public enum Encoding : byte
        {
            /// <summary>
            /// <see cref="OtherEncodings.Iso88591"/>.
            /// </summary>
            Iso88591 = 0,
            /// <summary>
            /// UTF-16.
            /// </summary>
            Utf16 = 1,
            /// <summary>
            /// UTF-16 Big Endian.
            /// </summary>
            Utf16BE = 2
        }
    }
}