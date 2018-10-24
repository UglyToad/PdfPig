namespace UglyToad.PdfPig.Fonts.Type1.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IO;
    using PdfPig.Parser.Parts;
    using Tokenization.Tokens;
    using Util;

    internal class Type1EncryptedPortionParser
    {
        private const ushort EexecEncryptionKey = 55665;
        private const int EexecRandomBytes = 4;

        public IReadOnlyList<byte> Parse(IReadOnlyList<byte> bytes)
        {
            if (!IsBinary(bytes))
            {
                bytes = ConvertHexToBinary(bytes);
            }

            var decrypted = Decrypt(bytes, EexecEncryptionKey, EexecRandomBytes);

            // line 461 of type1parser.java
            var str = OtherEncodings.BytesAsLatin1String(decrypted.ToArray());

            var tokenizer = new Type1Tokenizer(new ByteArrayInputBytes(decrypted));
            /*
             * After 4 random characters follows the /Private dictionary and the /CharString dictionary.
             * The first defines a number of technical terms involving character construction, and contains also an array of subroutines used in character paths.
             * The second contains the character descriptions themselves.
             * Both the subroutines and the character descriptions are yet again encrypted in a fashion similar to the entire binary segment, but now with an initial value of R = 4330 instead of 55665.
             */

            while (!tokenizer.CurrentToken.IsPrivateDictionary)
            {
                tokenizer.GetNext();
                if (tokenizer.CurrentToken == null)
                {
                    throw new InvalidOperationException("Did not find the private dictionary start token.");
                }
            }

            var next = tokenizer.GetNext();
            if (next?.Type != Type1Token.TokenType.Integer || !(next is Type1TextToken textToken))
            {
                throw new InvalidOperationException($"No length token was present in the stream following the private dictionary start, instead got {next}.");
            }

            var length = textToken.AsInt();
            ReadExpected(tokenizer, Type1Token.TokenType.Name, "dict");
            // actually could also be "/Private 10 dict def Private begin"
            // instead of the "dup"
            ReadExpected(tokenizer, Type1Token.TokenType.Name, "dup");
            ReadExpected(tokenizer, Type1Token.TokenType.Name, "begin");

            while (tokenizer.CurrentToken != null)
            {
                tokenizer.GetNext();
            }

            return decrypted;
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
            /*
             * We start with three constants R = 55665, c1 = 52845 and c2 = 22719.
             * Then we apply to the entire binary array c[i] of length n the decryption procedure:
             * for in [0, n):
             *    p[i] = c[i]^(R >> 8)
             *    R = ((c[i] + R)*c1 + c2) & ((1 << 16) - 1)
             *
             * Here ^ means xor addition, in which one interprets the bits modulo 2.
             * The encryption key R changes as the procedure is carried out.
             */
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

        private static void ReadExpected(Type1Tokenizer tokenizer, Type1Token.TokenType type, string text)
        {
            var token = tokenizer.GetNext();
            if (token == null)
            {
                throw new InvalidOperationException($"Type 1 Encrypted portion ended when a token with text '{text}' was expected instead.");
            }

            if (token.Type != type || !(token is Type1TextToken textToken) || !string.Equals(textToken.Text, text, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Found invalid token {token} when type {type} with text {text} was expected.");
            }
        }
    }
}
