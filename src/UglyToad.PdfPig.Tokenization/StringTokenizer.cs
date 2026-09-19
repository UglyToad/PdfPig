namespace UglyToad.PdfPig.Tokenization
{
    using System;
    using Core;
    using Tokens;

    internal sealed class StringTokenizer : ITokenizer
    {
        /// <summary>
        /// The bytes of the string being read, reused between tokens. A string in a file is a
        /// sequence of bytes, so they are gathered as bytes rather than as text that would have to
        /// be encoded straight back.
        /// </summary>
        private readonly ByteBuffer buffer = new ByteBuffer();

        public bool ReadsNextByte { get; } = false;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (inputBytes == null)
            {
                return false;
            }

            if (currentByte != '(')
            {
                return false;
            }

            var builder = buffer;
            var numberOfBrackets = 1;
            var isEscapeActive = false;
            var isLineBreaking = false;

            var octalModeActive = false;

            // The digits of an octal escape are read most significant first, so the value builds up
            // a digit at a time and needs nothing to hold them in.
            var octalValue = 0;
            var octalsRead = 0;

            while (inputBytes.MoveNext())
            {
                var b = inputBytes.CurrentByte;
                var c = (char)b;

                if (octalModeActive)
                {
                    var nextCharacterOctal = c >= '0' && c <= '7';

                    if (nextCharacterOctal)
                    {
                        octalValue = octalValue * 8 + c.CharacterToShort();
                        octalsRead++;
                    }

                    if (octalsRead == 3 || !nextCharacterOctal)
                    {
                        // An escape holds at most three octal digits, which reach \777. The
                        // specification (7.3.4.2) says high-order overflow is ignored, which is what
                        // truncating to a byte does.
                        builder.Append((byte)octalValue);

                        octalValue = 0;
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
                            builder.Append(b);
                        }

                        // TODO: Check for other ends of string where the string is improperly formatted. See commented method
                        numberOfBrackets = CheckForEndOfString(numberOfBrackets, inputBytes);

                        break;
                    case '(':
                        isLineBreaking = false;

                        if (!isEscapeActive)
                        {
                            numberOfBrackets++;
                        }

                        isEscapeActive = false;
                        builder.Append(b);
                        break;
                    // Escape
                    case '\\':
                        isLineBreaking = false;
                        // Escaped backslash
                        if (isEscapeActive)
                        {
                            builder.Append(b);
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
                            builder.Append(b);
                        }
                        else if (isEscapeActive)
                        {
                            ProcessEscapedCharacter(c, builder, ref octalValue, ref octalModeActive, ref octalsRead, ref isLineBreaking);
                            isEscapeActive = false;
                        }
                        else
                        {
                            builder.Append(b);
                        }

                        break;
                }

                if (numberOfBrackets <= 0)
                {
                    break;
                }
            }

            // The buffer holds the bytes of the string as it stands in the file. The token keeps
            // them and decodes its text only if something asks for it, since a text showing operand
            // is never text.
            token = new StringToken(builder.ToArrayAndClear());

            return true;
        }

        private static void ProcessEscapedCharacter(char c, ByteBuffer builder, ref int octalValue, ref bool isOctalActive,
            ref int octalsRead, ref bool isLineBreaking)
        {
            switch (c)
            {
                case 'n':
                    builder.Append((byte)'\n');
                    break;
                case 'r':
                    builder.Append((byte)'\r');
                    break;
                case 't':
                    builder.Append((byte)'\t');
                    break;
                case 'b':
                    builder.Append((byte)'\b');
                    break;
                case 'f':
                    builder.Append((byte)'\f');
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                    octalValue = c.CharacterToShort();
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
                        builder.Append((byte)c);
                    }
                    break;
            }
        }

        /// <summary>
        /// Gathers the bytes of one string, keeping its array between tokens so that reading a file
        /// full of them does not allocate one buffer each.
        /// </summary>
        private sealed class ByteBuffer
        {
            private byte[] bytes = new byte[64];

            private int length;

            public void Append(byte b)
            {
                if (length == bytes.Length)
                {
                    Array.Resize(ref bytes, bytes.Length * 2);
                }

                bytes[length++] = b;
            }

            public byte[] ToArrayAndClear()
            {
                var result = bytes.AsSpan(0, length).ToArray();

                length = 0;

                return result;
            }
        }

        private static int CheckForEndOfString(int numberOfBrackets, IInputBytes bytes)
        {
            const byte lineFeed = 10;
            const byte carriageReturn = 13;

            var braces = numberOfBrackets;
            var nextThreeBytes = new byte[3];

            var startAt = bytes.CurrentOffset;

            var amountRead = bytes.Read(nextThreeBytes);

            // Check the next 3 bytes if available
            // The following cases are valid indicators for the end of the string
            // 1. Next line contains another COSObject: CR + LF + '/'
            // 2. COSDictionary ends in the next line: CR + LF + '>'
            // 3. Next line contains another COSObject: CR + '/'
            // 4. COSDictionary ends in the next line: CR + '>'
            if (amountRead == 3 && nextThreeBytes[0] == carriageReturn)
            {
                if ((nextThreeBytes[1] == lineFeed && (nextThreeBytes[2] == '/') || nextThreeBytes[2] == '>')
                    || nextThreeBytes[1] == '/' || nextThreeBytes[1] == '>')
                {
                    braces = 0;
                }
            }

            if (amountRead > 0)
            {
                bytes.Seek(startAt);
            }

            return braces;
        }
    }
}
