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
        private const int Len4Bytes = 4;
        private const int Password = 5839;

        public IReadOnlyList<byte> Parse(IReadOnlyList<byte> bytes, bool isLenientParsing)
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
            if (next?.Type != Type1Token.TokenType.Integer)
            {
                throw new InvalidOperationException($"No length token was present in the stream following the private dictionary start, instead got {next}.");
            }

            var length = next.AsInt();
            ReadExpected(tokenizer, Type1Token.TokenType.Name, "dict");
            // actually could also be "/Private 10 dict def Private begin"
            // instead of the "dup"
            ReadExpectedAfterOptional(tokenizer, Type1Token.TokenType.Name, "def", Type1Token.TokenType.Name, "dup");
            ReadExpected(tokenizer, Type1Token.TokenType.Name, "begin");

            /*
             * The lenIV entry is an integer specifying the number of random bytes at the beginning of charstrings for charstring encryption.
             * The default value of lenIV is 4.
             */
            var lenIv = Len4Bytes;

            var builder = new Type1PrivateDictionary.Builder();

            for (var i = 0; i < length; i++)
            {
                var token = tokenizer.GetNext();
                // premature end
                if (token.Type != Type1Token.TokenType.Literal)
                {
                    break;
                }

                var key = token.Text;

                switch (key)
                {
                    case Type1Symbols.RdProcedure:
                        {
                            var procedureTokens = ReadProcedure(tokenizer);
                            ReadTillDef(tokenizer);
                            break;
                        }
                    case Type1Symbols.NoAccessDef:
                        {
                            var procedureTokens = ReadProcedure(tokenizer);
                            ReadTillDef(tokenizer);
                            break;
                        }
                    case Type1Symbols.NoAccessPut:
                        {
                            var procedureTokens = ReadProcedure(tokenizer);
                            ReadTillDef(tokenizer);
                            break;
                        }
                    case Type1Symbols.BlueValues:
                        {
                            var blueValues = ReadArrayValues(tokenizer, x => x.AsInt());
                            builder.BlueValues = blueValues;
                            break;
                        }
                    case Type1Symbols.OtherBlues:
                        {
                            var otherBlues = ReadArrayValues(tokenizer, x => x.AsInt());
                            builder.OtherBlues = otherBlues;
                            break;
                        }
                    case Type1Symbols.StdHorizontalStemWidth:
                        {
                            var widths = ReadArrayValues(tokenizer, x => x.AsDecimal());
                            var width = widths[0];
                            builder.StandardHorizontalWidth = width;
                            break;
                        }
                    case Type1Symbols.StdVerticalStemWidth:
                        {
                            var widths = ReadArrayValues(tokenizer, x => x.AsDecimal());
                            var width = widths[0];
                            builder.StandardVerticalWidth = width;
                            break;
                        }
                    case Type1Symbols.StemSnapHorizontalWidths:
                        {
                            var widths = ReadArrayValues(tokenizer, x => x.AsDecimal());
                            builder.StempSnapHorizontalWidths = widths;
                            break;
                        }
                    case Type1Symbols.StemSnapVerticalWidths:
                        {
                            var widths = ReadArrayValues(tokenizer, x => x.AsDecimal());
                            builder.StemSnapVerticalWidths = widths;
                            break;
                        }
                    case Type1Symbols.BlueScale:
                        {
                            builder.BlueScale = ReadNumeric(tokenizer);
                            break;
                        }
                    case Type1Symbols.ForceBold:
                        {
                            builder.ForceBold = ReadBoolean(tokenizer);
                            break;
                        }
                    case Type1Symbols.MinFeature:
                        {
                            var procedureTokens = ReadProcedure(tokenizer);

                            if (!isLenientParsing)
                            {
                                var valid = procedureTokens.Count == 2 && procedureTokens[0].AsInt() == 16
                                                                       && procedureTokens[1].AsInt() == 16;

                                if (!valid)
                                {
                                    var valueMessage = $"{{ {string.Join(", ", procedureTokens.Select(x => x.ToString()))} }}";
                                    throw new InvalidOperationException($"Type 1 font MinFeature should be {{16,16}} but got: {valueMessage}.");
                                }
                            }

                            break;
                        }
                    case Type1Symbols.Password:
                        {
                            var password = (int)ReadNumeric(tokenizer);
                            if (password != Password && !isLenientParsing)
                            {
                                throw new InvalidOperationException($"Type 1 font had the wrong password: {password}");
                            }

                            builder.Password = password;
                            break;
                        }
                    case Type1Symbols.UniqueId:
                        {
                            var id = (int)ReadNumeric(tokenizer);
                            builder.UniqueId = id;
                            break;
                        }
                    case Type1Symbols.Len4:
                        {
                            lenIv = (int)ReadNumeric(tokenizer);
                            break;
                        }
                    case Type1Symbols.BlueShift:
                        {
                            builder.BlueShift = (int)ReadNumeric(tokenizer);
                            break;
                        }
                    case Type1Symbols.BlueFuzz:
                        {
                            builder.BlueFuzz = (int)ReadNumeric(tokenizer);
                            break;
                        }
                    case Type1Symbols.FamilyBlues:
                        {
                            builder.FamilyBlues = ReadArrayValues(tokenizer, x => x.AsInt());
                            break;
                        }
                    case Type1Symbols.FamilyOtherBlues:
                        {
                            builder.FamilyOtherBlues = ReadArrayValues(tokenizer, x => x.AsInt());
                            break;
                        }
                    case Type1Symbols.LanguageGroup:
                        {
                            builder.LanguageGroup = (int)ReadNumeric(tokenizer);
                            break;
                        }
                    case Type1Symbols.RndStemUp:
                        {
                            builder.RoundStemUp = ReadBoolean(tokenizer);
                            break;
                        }
                    case Type1Symbols.Subroutines:
                        {
                            //readSubrs(lenIV);
                            break;

                        }
                    case Type1Symbols.OtherSubroutines:
                        {
                            ReadOtherSubroutines(tokenizer, isLenientParsing);
                            break;
                        }
                }
            }

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

        private static void ReadExpected(Type1Tokenizer tokenizer, Type1Token.TokenType type, string text = null)
        {
            var token = tokenizer.GetNext();
            if (token == null)
            {
                throw new InvalidOperationException($"Type 1 Encrypted portion ended when a token with text '{text}' was expected.");
            }

            if (token.Type != type || (text != null && !string.Equals(token.Text, text, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Found invalid token {token} when type {type} with text {text} was expected.");
            }
        }

        private static void ReadExpectedAfterOptional(Type1Tokenizer tokenizer, Type1Token.TokenType optionalType, string optionalText,
            Type1Token.TokenType type, string text)
        {
            var token = tokenizer.GetNext();
            if (token == null)
            {
                throw new InvalidOperationException($"Type 1 Encrypted portion ended when a token with text '{optionalText}' or '{text}' was expected.");
            }

            if (token.Type == type && string.Equals(token.Text, text, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (token.Type == optionalType && string.Equals(token.Text, optionalText, StringComparison.OrdinalIgnoreCase))
            {
                ReadExpected(tokenizer, type, text);
                return;
            }

            throw new InvalidOperationException($"Found invalid token {token} when type {type} with text {text} was expected.");
        }

        private static IReadOnlyList<Type1Token> ReadProcedure(Type1Tokenizer tokenizer)
        {
            var tokens = new List<Type1Token>();
            var depth = -1;
            ReadProcedure(tokenizer, tokens, ref depth);
            return tokens;
        }

        private static void ReadProcedure(Type1Tokenizer tokenizer, List<Type1Token> tokens, ref int depth)
        {
            if (depth == -1)
            {
                ReadExpected(tokenizer, Type1Token.TokenType.StartProc);
                depth = 1;
            }

            if (depth == 0)
            {
                return;
            }

            Type1Token token;
            while ((token = tokenizer.GetNext()) != null)
            {
                if (token.Type == Type1Token.TokenType.StartProc)
                {
                    depth += 1;
                    ReadProcedure(tokenizer, tokens, ref depth);
                }
                else if (token.Type == Type1Token.TokenType.EndProc)
                {
                    depth--;
                    break;
                }
                else
                {
                    tokens.Add(token);
                }
            }
        }

        private static void ReadTillDef(Type1Tokenizer tokenizer)
        {
            Type1Token token;
            while ((token = tokenizer.GetNext()) != null)
            {
                if (token.Type == Type1Token.TokenType.Name)
                {
                    if (string.Equals(token.Text, "def", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Encountered unexpected non-name token while reading till 'def' token: {token}");
                }
            }
        }

        private static void ReadTillPut(Type1Tokenizer tokenizer)
        {
            Type1Token token;
            while ((token = tokenizer.GetNext()) != null)
            {
                if (string.Equals(token.Text, "put", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                switch (token.Text)
                {
                    case "NP":
                    case "|":
                        return;
                }
            }
        }

        private static IReadOnlyList<T> ReadArrayValues<T>(Type1Tokenizer tokenizer, Func<Type1Token, T> converter, bool hasReadStart = false)
        {
            if (!hasReadStart)
            {
                ReadExpected(tokenizer, Type1Token.TokenType.StartArray);
            }

            var results = new List<T>();

            Type1Token token;
            while ((token = tokenizer.GetNext()) != null)
            {
                if (token.Type == Type1Token.TokenType.EndArray)
                {
                    break;
                }

                try
                {
                    var result = converter(token);

                    results.Add(result);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Conversion of token '{token}' to value of type {typeof(T).Name} failed.", ex);
                }
            }

            return results;
        }

        private static decimal ReadNumeric(Type1Tokenizer tokenizer)
        {
            var token = tokenizer.GetNext();

            if (token == null || (token.Type != Type1Token.TokenType.Integer && token.Type != Type1Token.TokenType.Real))
            {
                throw new InvalidOperationException($"Expected to read a numeric token, instead got: {token}.");
            }

            return token.AsDecimal();
        }

        private static bool ReadBoolean(Type1Tokenizer tokenizer)
        {
            var token = tokenizer.GetNext();

            if (token == null || (!string.Equals(token.Text, "true", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(token.Text, "false", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Expected to read a boolean token, instead got: {token}.");
            }

            return token.AsBool();
        }

        private static void ReadOtherSubroutines(Type1Tokenizer tokenizer, bool isLenientParsing)
        {
            var start = tokenizer.GetNext();

            if (start.Type == Type1Token.TokenType.StartArray)
            {
                ReadArrayValues(tokenizer, x => x, true);
            }
            else if (start.Type == Type1Token.TokenType.Integer || start.Type == Type1Token.TokenType.Real)
            {
                var length = start.AsInt();
                ReadExpected(tokenizer, Type1Token.TokenType.Name, "array");

                for (var i = 0; i < length; i++)
                {
                    ReadExpected(tokenizer, Type1Token.TokenType.Name, "dup");
                    ReadNumeric(tokenizer);
                    ReadTillPut(tokenizer);
                }
                ReadTillDef(tokenizer);
            }
            else if (!isLenientParsing)
            {
                throw new InvalidOperationException($"Failed to read start of /OtherSubrs array. Got start token: {start}.");
            }
        }
    }
}
