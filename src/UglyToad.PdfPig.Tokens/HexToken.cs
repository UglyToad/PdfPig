namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A token containing string data where the string is encoded as hexadecimal.
    /// </summary>
    public sealed class HexToken : IDataToken<string>
    {
        private static readonly Dictionary<char, byte> HexMap = new() {
            {'0', 0x00 },
            {'1', 0x01 },
            {'2', 0x02 },
            {'3', 0x03 },
            {'4', 0x04 },
            {'5', 0x05 },
            {'6', 0x06 },
            {'7', 0x07 },
            {'8', 0x08 },
            {'9', 0x09 },

            {'A', 0x0A },
            {'a', 0x0A },
            {'B', 0x0B },
            {'b', 0x0B },
            {'C', 0x0C },
            {'c', 0x0C },
            {'D', 0x0D },
            {'d', 0x0D },
            {'E', 0x0E },
            {'e', 0x0E },
            {'F', 0x0F },
            {'f', 0x0F }
        };

        /// <summary>
        /// The string contained in the hex data.
        /// </summary>
        public string Data { get; }

        private readonly byte[] _bytes;

        /// <summary>
        /// The bytes of the hex data.
        /// </summary>
        public ReadOnlySpan<byte> Bytes => _bytes;

        /// <summary>
        /// The memory of the hex data.
        /// </summary>
        public ReadOnlyMemory<byte> Memory => _bytes;

        /// <summary>
        /// Create a new <see cref="HexToken"/> from the provided hex characters.
        /// </summary>
        /// <param name="characters">A set of hex characters 0-9, A - F, a - f representing a string.</param>
        public HexToken(ReadOnlySpan<char> characters)
        {
            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            // if the final character is missing, it is considered to be a 0, as per 7.3.4.3
            // adding 1 to the characters array length ensure the size of the byte array is correct
            // in all situations
            var bytes = new byte[(characters.Length+1) / 2];
            int index = 0;

            for (var i = 0; i < characters.Length; i += 2)
            {
                char high = characters[i];
                char low;
                if (i == characters.Length - 1)
                {
                    low = '0';
                }
                else
                {
                    low = characters[i + 1];
                }

                var b = ConvertPair(high, low);
                bytes[index++] = b;
            }

            // Handle UTF-16BE format strings.
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                Data = Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            }
            else
            {
                var builder = new StringBuilder();

                foreach (var b in bytes)
                {
                    if (b != '\0')
                    {
                        builder.Append((char)b);
                    }
                }

                Data = builder.ToString();
            }

            _bytes = bytes;
        }

        /// <summary>
        /// Convert two hex characters to a byte.
        /// </summary>
        /// <param name="high">The high nibble.</param>
        /// <param name="low">The low nibble.</param>
        /// <returns>The byte.</returns>
        public static byte ConvertPair(char high, char low)
        {
            var highByte = HexMap[high];
            var lowByte = HexMap[low];

            return (byte)(highByte << 4 | lowByte);
        }

        /// <summary>
        /// Convert the bytes in this hex token to an integer.
        /// </summary>
        /// <param name="token">The token containing the data to convert.</param>
        /// <returns>The integer corresponding to the bytes.</returns>
        public static int ConvertHexBytesToInt(HexToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var bytes = token.Bytes;

            var value = bytes[0] & 0xFF;
            if (bytes.Length == 2)
            {
                value <<= 8;
                value += bytes[1] & 0xFF;
            }

            return value;
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is HexToken other))
            {
                return false;
            }

            return Data == other.Data;
        }

        /// <summary>
        /// Converts the binary data back to a hex string representation.
        /// </summary>
        public string GetHexString()
        {
#if NET8_0_OR_GREATER
            return Convert.ToHexString(Bytes);
#else
            return BitConverter.ToString(_bytes).Replace("-", string.Empty);
#endif
        }
    }
}