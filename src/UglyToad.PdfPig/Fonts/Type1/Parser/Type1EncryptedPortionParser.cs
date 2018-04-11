namespace UglyToad.PdfPig.Fonts.Type1.Parser
{
    using System.Collections.Generic;
    using System.Linq;
    using PdfPig.Parser.Parts;
    using Tokenization.Tokens;
    using Util;

    internal class Type1EncryptedPortionParser
    {
        private const ushort EexecEncryptionKey = 55665;
        private const int EexecRandomBytes = 4;

        public void Parse(IReadOnlyList<byte> bytes)
        {
            if (!IsBinary(bytes))
            {
                bytes = ConvertHexToBinary(bytes);
            }

            var decrypted = Decrypt(bytes, EexecEncryptionKey, EexecRandomBytes);

            var str = OtherEncodings.BytesAsLatin1String(decrypted.ToArray());
        }

        /// <summary>
        /// To distinguish between binary and hex the first 4 bytes (of the ciphertext) for hex must
        /// obey these restrictions:
        /// The first byte must not be whitespace.
        /// One of the first four ciphertext bytes must not be an ASCII hex character.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static bool IsBinary(IReadOnlyList<byte> bytes)
        {
            if (bytes.Count < 4)
            {
                return true;
            }

            if (ReadHelper.IsWhitespace(bytes[0]))
            {
                return true;
            }

            for (var i = 1; i < 4; i++)
            {
                var b = bytes[i];

                if (!ReadHelper.IsHex(b))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<byte> ConvertHexToBinary(IReadOnlyList<byte> bytes)
        {
            var result = new List<byte>(bytes.Count / 2);

            var last = '\0';
            var offset = 0;
            for (var i = 0; i < bytes.Count; i++)
            {
                var c = (char)bytes[i];
                if (!ReadHelper.IsHex(c))
                {
                    // TODO: do I need to assert this must be whitespace?
                    continue;
                }

                if (offset == 1)
                {
                    result.Add(HexToken.Convert(last, c));
                    offset = 0;
                }
                else
                {
                    offset++;
                }

                last = c;
            }

            return result;
        }

        private static IReadOnlyList<byte> Decrypt(IReadOnlyList<byte> bytes, int key, int randomBytes)
        {
            if (randomBytes == -1)
            {
                return bytes;
            }

            if (randomBytes > bytes.Count || bytes.Count == 0)
            {
                return new byte[0];
            }

            const int c1 = 52845;
            const int c2 = 22719;

            var plainBytes = new byte[bytes.Count - randomBytes];

            for (var i = 0; i < bytes.Count; i++)
            {
                var cipher = bytes[i] & 0xFF;
                var plain = cipher ^ key >> 8;

                if (i >= randomBytes)
                {
                    plainBytes[i - randomBytes] = (byte)plain;
                }

                key = (cipher + key) * c1 + c2 & 0xffff;
            }

            return plainBytes;
        }
    }
}
