namespace UglyToad.Pdf.Tokenization
{
    using System.Text;
    using IO;
    using Parser.Parts;
    using Tokens;

    public class StringTokenizer : ITokenizer
    {
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

            byte[] octal = { 0, 0, 0 };
            int octalsRead = 0;

            while (inputBytes.MoveNext())
            {
                var b = inputBytes.CurrentByte;
                var c = (char)b;

                if (octalModeActive && c >= '0' && c <= '7')
                {
                    if (octalsRead == 3)
                    {
                        var characterCode = FromOctal(octal);

                        // For now :(
                        // TODO: I have a sneaking suspicion this is wrong...
                        builder.Append((char)characterCode);

                        octal[0] = 0;
                        octal[1] = 0;
                        octal[2] = 0;
                        octalsRead = 0;
                    }
                    else
                    {
                        // left shift the octals.
                        LeftShiftOctal(b, octalsRead, octal);

                        octal[octalsRead] = b;
                        octalsRead++;
                    }
                }

                switch (c)
                {
                    case ')':
                        octalModeActive = false;
                        isLineBreaking = false;
                        if (!isEscapeActive)
                        {
                            numberOfBrackets--;
                        }

                        if (numberOfBrackets > 0)
                        {
                            builder.Append(c);

                            break;
                        }

                        // TODO: Check for other ends of string where the string is improperly formatted. See commented method
                        // numberOfBrackets = CheckForEndOfString(inputBytes, numberOfBrackets);

                        isEscapeActive = false;

                        break;
                    case '(':
                        octalModeActive = false;
                        isLineBreaking = false;



                        if (!isEscapeActive)
                        {
                            numberOfBrackets++;
                        }

                        builder.Append(c);
                        break;
                    // Escape
                    case '\\':
                        octalModeActive = false;
                        isLineBreaking = false;
                        // Escaped backslash
                        if (isEscapeActive)
                        {
                            builder.Append(c);
                        }
                        else
                        {
                            isEscapeActive = true;
                        }
                        break;
                    default:
                        octalModeActive = false;
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
            }

            token = new StringToken(builder.ToString());

            return true;
        }

        private static void LeftShiftOctal(byte nextOctalByte, int octalsRead, byte[] octals)
        {
            for (int i = octalsRead; i > 0; i--)
            {
                octals[i] = octals[i - 1];
            }

            octals[0] = nextOctalByte;
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

        private static void ProcessEscapedCharacter(char c, StringBuilder builder, byte[] octal, ref bool isOctalActive,
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
                    octal[0] = (byte)c;
                    isOctalActive = true;
                    octalsRead = 1;
                    break;
                case ')':
                    // TODO: Handle the weird malformed use case "/Something (C:\)"
                    // numberOfBrackets = CheckForEndOfString(inputBytes, numberOfBrackets);
                    builder.Append(c);
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

        private static int FromOctal(byte[] octal)
        {
            int Power(int x, int pow)
            {
                int ret = 1;
                while (pow != 0)
                {
                    if ((pow & 1) == 1)
                        ret *= x;
                    x *= x;
                    pow >>= 1;
                }

                return ret;
            }

            int sum = 0;
            for (int i = 0; i < octal.Length; i++)
            {
                var power = 2 - i;
                sum += octal[i] * Power(8, power);
            }

            return sum;
        }
    }
}