namespace UglyToad.Pdf.Parser
{
    using System;
    using System.IO;
    using System.Text;
    using Cos;
    using IO;
    using Util;

    /*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

    /**
     * This class is used to contain parsing logic that will be used by both the
     * PDFParser and the COSStreamParser.
     *
     * @author Ben Litchfield
     */
    public abstract class BaseParser
    {
        private static readonly long OBJECT_NUMBER_THRESHOLD = 10000000000L;

        private static readonly long GENERATION_NUMBER_THRESHOLD = 65535;

        static readonly int MAX_LENGTH_LONG = long.MaxValue.ToString().Length;

        /**
         * Log instance.
         */
        protected static readonly int E = 'e';
        protected static readonly int N = 'n';
        protected static readonly int D = 'd';

        protected static readonly int S = 's';
        protected static readonly int T = 't';
        protected static readonly int R = 'r';
        protected static readonly int A = 'a';
        protected static readonly int M = 'm';

        protected static readonly int O = 'o';
        protected static readonly int B = 'b';
        protected static readonly int J = 'j';

        /**
         * This is a string constant that will be used for comparisons.
         */
        public static readonly string DEF = "def";
        /**
         * This is a string constant that will be used for comparisons.
         */
        protected static readonly string ENDOBJ_string = "endobj";
        /**
         * This is a string constant that will be used for comparisons.
         */
        protected static readonly string ENDSTREAM_string = "endstream";
        /**
         * This is a string constant that will be used for comparisons.
         */
        protected static readonly string STREAM_string = "stream";
        /**
         * This is a string constant that will be used for comparisons.
         */
        private static readonly string TRUE = "true";
        /**
         * This is a string constant that will be used for comparisons.
         */
        private static readonly string FALSE = "false";
        /**
         * This is a string constant that will be used for comparisons.
         */
        private static readonly string NULL = "null";

        /**
         * ASCII code for line feed.
         */
        protected static readonly byte ASCII_LF = 10;
        /**
         * ASCII code for carriage return.
         */
        protected static readonly byte ASCII_CR = 13;
        private static readonly byte ASCII_ZERO = 48;
        private static readonly byte ASCII_NINE = 57;
        private static readonly byte ASCII_SPACE = 32;

        /**
         * This is the stream that will be read from.
         */
        protected readonly SequentialSource seqSource;

        /**
         * This is the document that will be parsed.
         */
        protected COSDocument document;

        /**
         * Default constructor.
         */
        public BaseParser(SequentialSource pdfSource)
        {
            this.seqSource = pdfSource;
        }

        private static bool isHexDigit(char ch)
        {
            return char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
        }
        
        protected void skipWhiteSpaces()
        {
            //PDF Ref 3.2.7 A stream must be followed by either
            //a CRLF or LF but nothing else.

            int whitespace = seqSource.read();

            //see brother_scan_cover.pdf, it adds whitespaces
            //after the stream but before the start of the
            //data, so just read those first
            while (ASCII_SPACE == whitespace)
            {
                whitespace = seqSource.read();
            }

            if (ASCII_CR == whitespace)
            {
                whitespace = seqSource.read();
                if (ASCII_LF != whitespace)
                {
                    seqSource.unread(whitespace);
                    //The spec says this is invalid but it happens in the real
                    //world so we must support it.
                }
            }
            else if (ASCII_LF != whitespace)
            {
                //we are in an error.
                //but again we will do a lenient parsing and just assume that everything
                //is fine
                seqSource.unread(whitespace);
            }
        }

        /**
         * This is really a bug in the Document creators code, but it caused a crash in PDFBox, the first bug was in this
         * format: /Title ( (5) /Creator which was patched in 1 place.
         *
         * However it missed the case where the number of opening and closing parenthesis isn't balanced
         *
         * The second bug was in this format /Title (c:\) /Producer
         *
         * This patch moves this code out of the parseCOSstring method, so it can be used twice.
         *
         * @param bracesParameter the number of braces currently open.
         *
         * @return the corrected value of the brace counter
         * @throws IOException
         */
        private int checkForEndOfstring(int bracesParameter)
        {
            int braces = bracesParameter;
            byte[]
            nextThreeBytes = new byte[3];
            int amountRead = seqSource.read(nextThreeBytes);

            // Check the next 3 bytes if available
            // The following cases are valid indicators for the end of the string
            // 1. Next line contains another COSObject: CR + LF + '/'
            // 2. CosDictionary ends in the next line: CR + LF + '>'
            // 3. Next line contains another COSObject: CR + '/'
            // 4. CosDictionary ends in the next line: CR + '>'
            if (amountRead == 3 && nextThreeBytes[0] == ASCII_CR)
            {
                if ((nextThreeBytes[1] == ASCII_LF && (nextThreeBytes[2] == '/') || nextThreeBytes[2] == '>')
                || nextThreeBytes[1] == '/' || nextThreeBytes[1] == '>')
                {
                    braces = 0;
                }
            }
            if (amountRead > 0)
            {
                seqSource.unread(nextThreeBytes, 0, amountRead);
            }
            return braces;
        }

        /**
         * This will parse a PDF string.
         *
         * @return The parsed PDF string.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected CosString parseCOSstring()
        {
            char nextChar = (char)seqSource.read();
            if (nextChar == '<')
            {
                return parseCOSHexstring();
            }
            else if (nextChar != '(')
            {
                throw new IOException("parseCOSstring string should start with '(' or '<' and not '" +
                nextChar + "' " + seqSource);
            }

            var charLf = (char)ASCII_LF;

            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream))
            {
                // This is the number of braces read
                int braces = 1;
                int c = seqSource.read();
                while (braces > 0 && c != -1)
                {
                    char ch = (char)c;
                    int nextc = -2; // not yet read

                    if (ch == ')')
                    {

                        braces--;
                        braces = checkForEndOfstring(braces);
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
                        char next = (char)seqSource.read();
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
                                braces = checkForEndOfstring(braces);
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
                                //case charLf:
                                // case ASCII_CR:
                                //this is a break in the line so ignore it and the newline and continue
                                c = seqSource.read();
                                while (isEOL(c) && c != -1)
                                {
                                    c = seqSource.read();
                                }
                                nextc = c;
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
                                    c = seqSource.read();
                                    char digit = (char)c;
                                    if (digit >= '0' && digit <= '7')
                                    {
                                        octal.Append(digit);
                                        c = seqSource.read();
                                        digit = (char)c;
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

                                    int character = 0;
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
                        c = seqSource.read();
                    }
                }
                if (c != -1)
                {
                    seqSource.unread(c);
                }
                return new CosString(memoryStream.ToArray());
            }

        }

        /**
         * This will parse a PDF HEX string with fail fast semantic
         * meaning that we stop if a not allowed character is found.
         * This is necessary in order to detect malformed input and
         * be able to skip to next object start.
         *
         * We assume starting '&lt;' was already read.
         * 
         * @return The parsed PDF string.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        private CosString parseCOSHexstring()
        {
            var sBuf = new StringBuilder();
            while (true)
            {
                int c = seqSource.read();
                if (isHexDigit((char)c))
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
                else if ((c == ' ') || (c == '\n') ||
                (c == '\t') || (c == '\r') ||
                (c == '\b') || (c == '\f'))
                {
                    continue;
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
                        c = seqSource.read();
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
        

        /**
         * Determine if a character terminates a PDF name.
         *
         * @param ch The character
         * @return true if the character terminates a PDF name, otherwise false.
         */
        protected bool isEndOfName(int ch)
        {
            return ch == ASCII_SPACE || ch == ASCII_CR || ch == ASCII_LF || ch == 9 || ch == '>' ||
            ch == '<' || ch == '[' || ch == '/' || ch == ']' || ch == ')' || ch == '(' ||
            ch == 0 || ch == '\f';
        }
        
        /**
         * Returns true if a byte sequence is valid UTF-8.
         */
        private bool isValidUTF8(byte[] input)
        {
            try
            {
                Decoder d = Encoding.UTF8.GetDecoder();
                var charLength = d.GetCharCount(input, 0, input.Length);
                var chars = new char[charLength];
                d.Convert(input, 0, input.Length, chars, 0, charLength, true, out _, out _, out _);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        /**
         * This will parse a bool object from the stream.
         *
         * @return The parsed bool object.
         *
         * @throws IOException If an IO error occurs during parsing.
         */
        protected CosBoolean parsebool()
        {
            CosBoolean retval = null;
            char c = (char)seqSource.peek();
            if (c == 't')
            {
                string truestring = OtherEncodings.BytesAsLatin1String(seqSource.readFully(4));
                if (!truestring.Equals(TRUE))
                {
                    throw new IOException("Error parsing bool: expected='true' actual='" + truestring
                    + "' at offset " + seqSource.getPosition());
                }
                else
                {
                    retval = CosBoolean.True;
                }
            }
            else if (c == 'f')
            {
                string falsestring = OtherEncodings.BytesAsLatin1String(seqSource.readFully(5));
                if (!falsestring.Equals(FALSE))
                {
                    throw new IOException("Error parsing bool: expected='true' actual='" + falsestring
                    + "' at offset " + seqSource.getPosition());
                }
                else
                {
                    retval = CosBoolean.False;
                }
            }
            else
            {
                throw new IOException("Error parsing bool expected='t or f' actual='" + c
                + "' at offset " + seqSource.getPosition());
            }
            return retval;
        }

        /**
         * This will read the next string from the stream.
         *
         * @return The string that was read from the stream, never null.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected string readstring()
        {
            SkipSpaces();
            StringBuilder buffer = new StringBuilder();
            int c = seqSource.read();
            while (!isEndOfName((char)c) && c != -1)
            {
                buffer.Append((char)c);
                c = seqSource.read();
            }
            if (c != -1)
            {
                seqSource.unread(c);
            }
            return buffer.ToString();
        }

        /**
         * Read one string and throw an exception if it is not the expected value.
         *
         * @param expectedstring the string value that is expected.
         * @throws IOException if the string char is not the expected value or if an
         * I/O error occurs.
         */
        protected void readExpectedstring(string expectedstring)
        {
            readExpectedstring(expectedstring, false);
        }

        /**
         * Reads given pattern from {@link #seqSource}. Skipping whitespace at start and end if wanted.
         * 
         * @param expectedstring pattern to be skipped
         * @param skipSpaces if set to true spaces before and after the string will be skipped
         * @throws IOException if pattern could not be read
         */
        protected void readExpectedstring(string expectedstring, bool skipSpaces)
        {
            SkipSpaces();
            foreach (var c in expectedstring)
            {
                if (seqSource.read() != c)
                {
                    throw new IOException("Expected string '" + expectedstring
                    + "' but missed at character '" + c + "' at offset "
                    + seqSource.getPosition());
                }
            }
            SkipSpaces();
        }

        /**
         * Read one char and throw an exception if it is not the expected value.
         *
         * @param ec the char value that is expected.
         * @throws IOException if the read char is not the expected value or if an
         * I/O error occurs.
         */
        protected void readExpectedChar(char ec)
        {
            char c = (char)seqSource.read();
            if (c != ec)
            {
                throw new IOException("expected='" + ec + "' actual='" + c + "' at offset " + seqSource.getPosition());
            }
        }

        /**
         * This will read the next string from the stream up to a certain length.
         *
         * @param length The length to stop reading at.
         *
         * @return The string that was read from the stream of length 0 to length.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected string readstring(int length)
        {
            SkipSpaces();

            int c = seqSource.read();

            //average string size is around 2 and the normal string buffer size is
            //about 16 so lets save some space.
            StringBuilder buffer = new StringBuilder(length);
            while (!isWhitespace(c) && !isClosing(c) && c != -1 && buffer.Length < length &&
            c != '[' &&
            c != '<' &&
            c != '(' &&
            c != '/')
            {
                buffer.Append((char)c);
                c = seqSource.read();
            }
            if (c != -1)
            {
                seqSource.unread(c);
            }
            return buffer.ToString();
        }

        /**
         * This will tell if the next character is a closing brace( close of PDF array ).
         *
         * @return true if the next byte is ']', false otherwise.
         *
         * @throws IOException If an IO error occurs.
         */
        protected bool isClosing()
        {
            return isClosing(seqSource.peek());
        }

        /**
         * This will tell if the next character is a closing brace( close of PDF array ).
         *
         * @param c The character to check against end of line
         * @return true if the next byte is ']', false otherwise.
         */
        protected bool isClosing(int c)
        {
            return c == ']';
        }

        /**
         * This will read bytes until the first end of line marker occurs.
         * NOTE: The EOL marker may consists of 1 (CR or LF) or 2 (CR and CL) bytes
         * which is an important detail if one wants to unread the line.
         *
         * @return The characters between the current position and the end of the line.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected string readLine()
        {
            if (seqSource.isEOF())
            {
                throw new IOException("Error: End-of-File, expected line");
            }

            StringBuilder buffer = new StringBuilder(11);

            int c;
            while ((c = seqSource.read()) != -1)
            {
                // CR and LF are valid EOLs
                if (isEOL(c))
                {
                    break;
                }
                buffer.Append((char)c);
            }
            // CR+LF is also a valid EOL 
            if (isCR(c) && isLF(seqSource.peek()))
            {
                seqSource.read();
            }
            return buffer.ToString();
        }

        /**
         * This will tell if the next byte to be read is an end of line byte.
         *
         * @return true if the next byte is 0x0A or 0x0D.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected bool isEOL()
        {
            return isEOL(seqSource.peek());
        }

        /**
         * This will tell if the next byte to be read is an end of line byte.
         *
         * @param c The character to check against end of line
         * @return true if the next byte is 0x0A or 0x0D.
         */
        protected bool isEOL(int c)
        {
            return isLF(c) || isCR(c);
        }

        private bool isLF(int c)
        {
            return ASCII_LF == c;
        }

        private bool isCR(int c)
        {
            return ASCII_CR == c;
        }

        /**
         * This will tell if the next byte is whitespace or not.
         *
         * @return true if the next byte in the stream is a whitespace character.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected bool isWhitespace()
        {
            return isWhitespace(seqSource.peek());
        }

        /**
         * This will tell if a character is whitespace or not.  These values are
         * specified in table 1 (page 12) of ISO 32000-1:2008.
         * @param c The character to check against whitespace
         * @return true if the character is a whitespace character.
         */
        protected bool isWhitespace(int c)
        {
            return c == 0 || c == 9 || c == 12 || c == ASCII_LF
            || c == ASCII_CR || c == ASCII_SPACE;
        }

        /**
         * This will tell if the next byte is a space or not.
         *
         * @return true if the next byte in the stream is a space character.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected bool isSpace()
        {
            return isSpace(seqSource.peek());
        }

        /**
         * This will tell if the given value is a space or not.
         * 
         * @param c The character to check against space
         * @return true if the next byte in the stream is a space character.
         */
        protected bool isSpace(int c)
        {
            return ASCII_SPACE == c;
        }

        /**
         * This will tell if the next byte is a digit or not.
         *
         * @return true if the next byte in the stream is a digit.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected bool isDigit()
        {
            return isDigit(seqSource.peek());
        }

        /**
         * This will tell if the given value is a digit or not.
         * 
         * @param c The character to be checked
         * @return true if the next byte in the stream is a digit.
         */
        protected static bool isDigit(int c)
        {
            return c >= ASCII_ZERO && c <= ASCII_NINE;
        }

        /**
         * This will skip all spaces and comments that are present.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected void SkipSpaces()
        {
            int c = seqSource.read();
            // 37 is the % character, a comment
            while (isWhitespace(c) || c == 37)
            {
                if (c == 37)
                {
                    // skip past the comment section
                    c = seqSource.read();
                    while (!isEOL(c) && c != -1)
                    {
                        c = seqSource.read();
                    }
                }
                else
                {
                    c = seqSource.read();
                }
            }
            if (c != -1)
            {
                seqSource.unread(c);
            }
        }

        /**
         * This will read a long from the Stream and throw an {@link IOException} if
         * the long value is negative or has more than 10 digits (i.e. : bigger than
         * {@link #OBJECT_NUMBER_THRESHOLD})
         *
         * @return the object number being read.
         * @throws IOException if an I/O error occurs
         */
        protected long readObjectNumber()
        {
            long retval = readLong();
            if (retval < 0 || retval >= OBJECT_NUMBER_THRESHOLD)
            {
                throw new IOException("Object Number '" + retval + "' has more than 10 digits or is negative");
            }
            return retval;
        }

        /**
         * This will read a integer from the Stream and throw an {@link IllegalArgumentException} if the integer value
         * has more than the maximum object revision (i.e. : bigger than {@link #GENERATION_NUMBER_THRESHOLD})
         * @return the generation number being read.
         * @throws IOException if an I/O error occurs
         */
        protected int readGenerationNumber()
        {
            int retval = readInt();
            if (retval < 0 || retval > GENERATION_NUMBER_THRESHOLD)
            {
                throw new IOException("Generation Number '" + retval + "' has more than 5 digits");
            }
            return retval;
        }

        /**
         * This will read an integer from the stream.
         *
         * @return The integer that was read from the stream.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected int readInt()
        {
            SkipSpaces();
            int retval = 0;

            StringBuilder intBuffer = readstringNumber();

            try
            {
                retval = int.Parse(intBuffer.ToString());
            }
            catch (FormatException e)
            {
                seqSource.unread(OtherEncodings.StringAsLatin1Bytes(intBuffer.ToString()));
                throw new IOException("Error: Expected an integer type at offset " + seqSource.getPosition(), e);
            }
            return retval;
        }


        /**
         * This will read an long from the stream.
         *
         * @return The long that was read from the stream.
         *
         * @throws IOException If there is an error reading from the stream.
         */
        protected long readLong()
        {
            SkipSpaces();
            long retval = 0;

            StringBuilder longBuffer = readstringNumber();

            try
            {
                retval = long.Parse(longBuffer.ToString());
            }
            catch (FormatException e)
            {
                seqSource.unread(OtherEncodings.StringAsLatin1Bytes(longBuffer.ToString()));

                throw new IOException(
                    $"Error: Expected a long type at offset {seqSource.getPosition()}, instead got \'{longBuffer}\'", e);
            }

            return retval;
        }

        /**
         * This method is used to read a token by the {@linkplain #readInt()} method
         * and the {@linkplain #readLong()} method.
         *
         * @return the token to parse as integer or long by the calling method.
         * @throws IOException throws by the {@link #seqSource} methods.
         */
        protected StringBuilder readstringNumber()
        {
            int lastByte = 0;
            StringBuilder buffer = new StringBuilder();
            while ((lastByte = seqSource.read()) != ASCII_SPACE &&
            lastByte != ASCII_LF &&
            lastByte != ASCII_CR &&
            lastByte != 60 && //see sourceforge bug 1714707
            lastByte != '[' && // PDFBOX-1845
            lastByte != '(' && // PDFBOX-2579
            lastByte != 0 && //See sourceforge bug 853328
            lastByte != -1)
            {
                buffer.Append((char)lastByte);
                if (buffer.Length > MAX_LENGTH_LONG)
                {
                    throw new IOException("Number '" + buffer +
                    "' is getting too long, stop reading at offset " + seqSource.getPosition());
                }
            }
            if (lastByte != -1)
            {
                seqSource.unread(lastByte);
            }
            return buffer;
        }
    }

}
