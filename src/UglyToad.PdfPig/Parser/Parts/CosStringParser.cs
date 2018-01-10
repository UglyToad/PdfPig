namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using System.IO;
    using System.Text;
    using Cos;
    using IO;

    internal class CosStringParser
    {
        public CosString Parse(IRandomAccessRead seqSource)
        {
            char nextChar = (char)seqSource.Read();
            if (nextChar == '<')
            {
                return ParseHexString(seqSource);
            }

            if (nextChar != '(')
            {
                throw new IOException("parseCOSstring string should start with '(' or '<' and not '" +
                nextChar + "' " + seqSource);
            }
            
            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream))
            {
                // This is the number of braces read
                int braces = 1;
                int c = seqSource.Read();
                while (braces > 0 && c != -1)
                {
                    char ch = (char) c;
                    int nextc = -2; // not yet read

                    if (ch == ')')
                    {

                        braces--;
                        braces = CheckForEndOfString(seqSource, braces);
                        if (braces != 0)
                        {
                            writer.Write(ch);
                        }
                    }
                    else if (ch == '(')
                    {
                        braces++;
                        writer.Write(ch);
                    }
                    else if (ch == '\\')
                    {
                        //patched by ram
                        char next = (char) seqSource.Read();
                        switch (next)
                        {
                            case 'n':
                                writer.Write('\n');
                                break;
                            case 'r':
                                writer.Write('\r');
                                break;
                            case 't':
                                writer.Write('\t');
                                break;
                            case 'b':
                                writer.Write('\b');
                                break;
                            case 'f':
                                writer.Write('\f');
                                break;
                            case ')':
                                // PDFBox 276 /Title (c:\)
                                braces = CheckForEndOfString(seqSource, braces);
                                if (braces != 0)
                                {
                                    writer.Write(next);
                                }
                                else
                                {
                                    writer.Write('\\');
                                }
                                break;
                            case '(':
                            case '\\':
                                writer.Write(next);
                                break;
                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            {
                                var octal = new StringBuilder();
                                octal.Append(next);
                                c = seqSource.Read();
                                char digit = (char) c;
                                if (digit >= '0' && digit <= '7')
                                {
                                    octal.Append(digit);
                                    c = seqSource.Read();
                                    digit = (char) c;
                                    if (digit >= '0' && digit <= '7')
                                    {
                                        octal.Append(digit);
                                    }
                                    else
                                    {
                                        nextc = c;
                                    }
                                }
                                else
                                {
                                    nextc = c;
                                }

                                int character;
                                try
                                {
                                    character = Convert.ToInt32(octal.ToString(), 8);
                                }
                                catch (FormatException e)
                                {
                                    throw new IOException("Error: Expected octal character, actual='" + octal + "'", e);
                                }

                                writer.Write(character);
                                break;
                            }
                            default:
                                if (c == ReadHelper.AsciiCarriageReturn || c == ReadHelper.AsciiLineFeed)
                                {
                                    // this is a break in the line so ignore it and the newline and continue
                                    c = seqSource.Read();
                                    while (ReadHelper.IsEndOfLine(c) && c != -1)
                                    {
                                        c = seqSource.Read();
                                    }

                                    nextc = c;

                                    break;
                                }
                                // dropping the backslash
                                // see 7.3.4.2 Literal strings for further information
                                writer.Write(next);
                                break;

                        }
                    }
                    else
                    {
                        writer.Write(ch);
                    }
                    if (nextc != -2)
                    {
                        c = nextc;
                    }
                    else
                    {
                        c = seqSource.Read();
                    }
                }
                if (c != -1)
                {
                    seqSource.Unread(c);
                }
                writer.Flush();
                return new CosString(memoryStream.ToArray());
            }
        }

        private static int CheckForEndOfString(IRandomAccessRead reader, int bracesParameter)
        {
            int braces = bracesParameter;
            byte[] nextThreeBytes = new byte[3];
            int amountRead = reader.Read(nextThreeBytes);

            // Check the next 3 bytes if available
            // The following cases are valid indicators for the end of the string
            // 1. Next line contains another COSObject: CR + LF + '/'
            // 2. CosDictionary ends in the next line: CR + LF + '>'
            // 3. Next line contains another COSObject: CR + '/'
            // 4. CosDictionary ends in the next line: CR + '>'
            if (amountRead == 3 && nextThreeBytes[0] == ReadHelper.AsciiCarriageReturn)
            {
                if (nextThreeBytes[1] == ReadHelper.AsciiLineFeed && nextThreeBytes[2] == '/' || nextThreeBytes[2] == '>'
                    || nextThreeBytes[1] == '/' || nextThreeBytes[1] == '>')
                {
                    braces = 0;
                }
            }
            if (amountRead > 0)
            {
                reader.Unread(nextThreeBytes, 0, amountRead);
            }
            return braces;
        }

        /// <summary>
        /// This will parse a PDF HEX string with fail fast semantic meaning that we stop if a not allowed character is found.
        /// This is necessary in order to detect malformed input and be able to skip to next object start.
        /// We assume starting '&lt;' was already read.
        /// </summary>
        private static CosString ParseHexString(IRandomAccessRead reader)
        {
            var sBuf = new StringBuilder();
            while (true)
            {
                int c = reader.Read();
                if (ReadHelper.IsHexDigit((char)c))
                {
                    sBuf.Append((char)c);
                }
                else if (c == '>')
                {
                    break;
                }
                else if (c < 0)
                {
                    throw new IOException("Missing closing bracket for hex string. Reached EOS.");
                }
                else if (c == ' ' || c == '\n' || c == '\t' || c == '\r' || c == '\b' || c == '\f')
                {
                }
                else
                {
                    // if invalid chars was found: discard last
                    // hex character if it is not part of a pair
                    if (sBuf.Length % 2 != 0)
                    {
                        sBuf.Remove(sBuf.Length - 1, 1);
                    }

                    // read till the closing bracket was found
                    do
                    {
                        c = reader.Read();
                    }
                    while (c != '>' && c >= 0);

                    // might have reached EOF while looking for the closing bracket
                    // this can happen for malformed PDFs only. Make sure that there is
                    // no endless loop.
                    if (c < 0)
                    {
                        throw new IOException("Missing closing bracket for hex string. Reached EOS.");
                    }

                    // exit loop
                    break;
                }
            }
            return CosString.ParseHex(sBuf.ToString());
        }
    }
}
