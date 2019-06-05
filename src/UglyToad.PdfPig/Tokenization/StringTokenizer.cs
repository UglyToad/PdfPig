namespace UglyToad.PdfPig.Tokenization
{
    using System.Text;
    using IO;
    using Parser.Parts;
    using Tokens;
    using Util;

    internal class StringTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = false;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            var builder = new StringBuilder();
            token = null;

            if (inputBytes == null)
            {
                return false;
            }

            if (currentByte != '(')
            {
                return false;
            }

            int numberOfBrackets = 1;
            bool isEscapeActive = false;
            bool isLineBreaking = false;

            bool octalModeActive = false;

            short[] octal = { 0, 0, 0 };
            int octalsRead = 0;

            while (inputBytes.MoveNext())
            {
                var b = inputBytes.CurrentByte;
                var c = (char)b;

                if (octalModeActive)
                {
                    var nextCharacterOctal = c >= '0' && c <= '7';

                    if (nextCharacterOctal)
                    {
                        // left shift the octals.
                        LeftShiftOctal(c, octalsRead, octal);
                        octalsRead++;
                    }

                    if (octalsRead == 3 || !nextCharacterOctal)
                    {
                        var characterCode = OctalHelpers.FromOctalDigits(octal);

                        // For now :(
                        // TODO: I have a sneaking suspicion this is wrong, not sure what behaviour is for large octal numbers
                        builder.Append((char)characterCode);

                        octal[0] = 0;
                        octal[1] = 0;
                        octal[2] = 0;
                        octalsRead = 0;
                        octalModeActive = false;
                    }

                    if (nextCharacterOctal)
                    {
                        continue;
                    }
                }

                switch (c)
                {
                    case ')':
                        isLineBreaking = false;
                        if (!isEscapeActive)
                        {
                            numberOfBrackets--;
                        }

                        isEscapeActive = false;
                        if (numberOfBrackets > 0)
                        {
                            builder.Append(c);
                        }

                        // TODO: Check for other ends of string where the string is improperly formatted. See commented method
                        // numberOfBrackets = CheckForEndOfString(inputBytes, numberOfBrackets);


                        break;
                    case '(':
                        isLineBreaking = false;
                        
                        if (!isEscapeActive)
                        {
                            numberOfBrackets++;
                        }

                        isEscapeActive = false;
                        builder.Append(c);
                        break;
                    // Escape
                    case '\\':
                        isLineBreaking = false;
                        // Escaped backslash
                        if (isEscapeActive)
                        {
                            builder.Append(c);
                            isEscapeActive = false;
                        }
                        else
                        {
                            isEscapeActive = true;
                        }
                        break;
                    default:
                        if (isLineBreaking)
                        {
                            if (ReadHelper.IsEndOfLine(c))
                            {
                                continue;
                            }

                            isLineBreaking = false;
                            builder.Append(c);
                        }
                        else if (isEscapeActive)
                        {
                            ProcessEscapedCharacter(c, builder, octal, ref octalModeActive, ref octalsRead, ref isLineBreaking);
                            isEscapeActive = false;
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }

                if (numberOfBrackets <= 0)
                {
                    break;
                }
            }

            string tokenStr;
            if (builder.Length >= 2)
            {
                if (builder[0] == 0xFE && builder[1] == 0xFF)
                {
                    var rawBytes = OtherEncodings.StringAsLatin1Bytes(builder.ToString());

                    tokenStr = Encoding.BigEndianUnicode.GetString(rawBytes);
                }
                else if (builder[0] == 0xFF && builder[1] == 0xFE)
                {
                    var rawBytes = OtherEncodings.StringAsLatin1Bytes(builder.ToString());

                    tokenStr = Encoding.Unicode.GetString(rawBytes);
                }
                else
                {
                    tokenStr = builder.ToString();
                }
            }
            else
            {
                tokenStr = builder.ToString();
            }

            token = new StringToken(tokenStr);

            return true;
        }

        private static void LeftShiftOctal(char nextOctalChar, int octalsRead, short[] octals)
        {
            for (int i = octalsRead; i > 0; i--)
            {
                octals[i] = octals[i - 1];
            }

            var value = nextOctalChar.CharacterToShort();

            octals[0] = value;
        }

        //private static int CheckForEndOfString(IRandomAccessRead reader, int bracesParameter)
        //{
        //    int braces = bracesParameter;
        //    byte[] nextThreeBytes = new byte[3];
        //    int amountRead = reader.Read(nextThreeBytes);

        //    // Check the next 3 bytes if available
        //    // The following cases are valid indicators for the end of the string
        //    // 1. Next line contains another COSObject: CR + LF + '/'
        //    // 2. CosDictionary ends in the next line: CR + LF + '>'
        //    // 3. Next line contains another COSObject: CR + '/'
        //    // 4. CosDictionary ends in the next line: CR + '>'
        //    if (amountRead == 3 && nextThreeBytes[0] == ReadHelper.AsciiCarriageReturn)
        //    {
        //        if (nextThreeBytes[1] == ReadHelper.AsciiLineFeed && nextThreeBytes[2] == '/' || nextThreeBytes[2] == '>'
        //            || nextThreeBytes[1] == '/' || nextThreeBytes[1] == '>')
        //        {
        //            braces = 0;
        //        }
        //    }
        //    if (amountRead > 0)
        //    {
        //        reader.Unread(nextThreeBytes, 0, amountRead);
        //    }
        //    return braces;
        //}
        //}

        private static void ProcessEscapedCharacter(char c, StringBuilder builder, short[] octal, ref bool isOctalActive,
            ref int octalsRead, ref bool isLineBreaking)
        {
            switch (c)
            {
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'b':
                    builder.Append('\b');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                    octal[0] = c.CharacterToShort();
                    isOctalActive = true;
                    octalsRead = 1;
                    break;
                default:
                    if (c == ReadHelper.AsciiCarriageReturn || c == ReadHelper.AsciiLineFeed)
                    {
                        isLineBreaking = true;
                    }
                    else
                    {
                        // Drop the backslash
                        builder.Append(c);
                    }
                    break;
            }
        }
    }
}