namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using IO;
    using Util;

    internal static class ReadHelper
    {
        public const byte AsciiLineFeed = 10;
        public const byte AsciiCarriageReturn = 13;

        public static string ReadLine(IRandomAccessRead reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.IsEof())
            {
                throw new InvalidOperationException("Error: End-of-File, expected line");
            }

            var buffer = new StringBuilder(11);

            int c;
            while ((c = reader.Read()) != -1)
            {
                // CR and LF are valid EOLs
                if (IsEndOfLine(c))
                {
                    break;
                }

                buffer.Append((char)c);
            }

            // CR+LF is also a valid EOL 
            if (IsCarriageReturn(c) && IsLineFeed(reader.Peek()))
            {
                reader.Read();
            }

            return buffer.ToString();
        }

        public static string ReadString(IRandomAccessRead reader)
        {
            SkipSpaces(reader);
            StringBuilder buffer = new StringBuilder();
            int c = reader.Read();
            while (!IsEndOfName((char)c) && c != -1)
            {
                buffer.Append((char)c);
                c = reader.Read();
            }
            if (c != -1)
            {
                reader.Unread(c);
            }

            return buffer.ToString();
        }

        public static void SkipSpaces(IRandomAccessRead reader)
        {
            const int commentCharacter = 37;
            int c = reader.Read();

            while (IsWhitespace(c) || c == 37)
            {
                if (c == commentCharacter)
                {
                    // skip past the comment section
                    c = reader.Read();
                    while (!IsEndOfLine(c) && c != -1)
                    {
                        c = reader.Read();
                    }
                }
                else
                {
                    c = reader.Read();
                }
            }
            if (c != -1)
            {
                reader.Unread(c);
            }
        }

        private static readonly HashSet<int> EndOfNameCharacters = new HashSet<int>
        {
            ' ',
            AsciiCarriageReturn,
            AsciiLineFeed,
            9,
            '>',
            '<',
            '[',
            '/',
            ']',
            ')',
            '(',
            0,
            '\f'
        };

        public static bool IsEndOfName(int ch)
        {
            return EndOfNameCharacters.Contains(ch);
        }

        /// <summary>
        /// Determines if the current character in the reader is a whitespace.
        /// </summary>
        public static bool IsWhitespace(IRandomAccessRead reader)
        {
            return IsWhitespace(reader.Peek());
        }

        /// <summary>
        /// Determines if a character is whitespace or not.
        /// </summary>
        /// <remarks>
        /// These values are specified in table 1 (page 12) of ISO 32000-1:2008.
        /// </remarks>
        public static bool IsWhitespace(int c)
        {
            return c == 0 || c == 9 || c == 12 || c == AsciiLineFeed
                   || c == AsciiCarriageReturn || c == ' ';
        }

        public static bool IsEndOfLine(int c)
        {
            return IsLineFeed(c) || IsCarriageReturn(c);
        }

        public static bool IsEndOfLine(byte b)
        {
            return IsLineFeed(b) || IsCarriageReturn(b);
        }

        public static bool IsLineFeed(int c)
        {
            return AsciiLineFeed == c;
        }

        public static bool IsCarriageReturn(int c)
        {
            return AsciiCarriageReturn == c;
        }

        public static bool IsString(IRandomAccessRead reader, string str) => IsString(reader, str.Select(x => (byte)x));
        public static bool IsString(IRandomAccessRead reader, IEnumerable<byte> str)
        {
            bool bytesMatching = true;
            long originOffset = reader.GetPosition();
            foreach (var c in str)
            {
                if (reader.Read() != c)
                {
                    bytesMatching = false;
                    break;
                }
            }
            reader.Seek(originOffset);

            return bytesMatching;
        }

        public static long ReadLong(IRandomAccessRead reader)
        {
            SkipSpaces(reader);
            long retval;

            StringBuilder longBuffer = ReadStringNumber(reader);

            try
            {
                retval = long.Parse(longBuffer.ToString());
            }
            catch (FormatException e)
            {
                var bytesToReverse = OtherEncodings.StringAsLatin1Bytes(longBuffer.ToString());
                reader.Unread(bytesToReverse);

                throw new InvalidOperationException($"Error: Expected a long type at offset {reader.GetPosition()}, instead got \'{longBuffer}\'", e);
            }

            return retval;
        }

        private static StringBuilder ReadStringNumber(IRandomAccessRead reader)
        {
            int lastByte = 0;
            StringBuilder buffer = new StringBuilder();
            while ((lastByte = reader.Read()) != ' ' &&
                   lastByte != AsciiLineFeed &&
                   lastByte != AsciiCarriageReturn &&
                   lastByte != 60 && //see sourceforge bug 1714707
                   lastByte != '[' && // PDFBOX-1845
                   lastByte != '(' && // PDFBOX-2579
                   lastByte != 0 && //See sourceforge bug 853328
                   lastByte != -1)
            {
                buffer.Append((char)lastByte);
                if (buffer.Length > long.MaxValue.ToString("D").Length)
                {
                    throw new IOException("Number '" + buffer + "' is getting too long, stop reading at offset " + reader.GetPosition());
                }
            }
            if (lastByte != -1)
            {
                reader.Unread(lastByte);
            }

            return buffer;
        }

        public static bool IsDigit(IRandomAccessRead reader)
        {
            return IsDigit(reader.Peek());
        }

        /// <summary>
        /// This will tell if the given value is a digit or not.
        /// </summary>
        public static bool IsDigit(int c)
        {
            return c >= '0' && c <= '9';
        }

        public static int ReadInt(IRandomAccessRead reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            SkipSpaces(reader);
            int result;

            var intBuffer = ReadStringNumber(reader);

            try
            {
                result = int.Parse(intBuffer.ToString());
            }
            catch (Exception e)
            {
                reader.Unread(OtherEncodings.StringAsLatin1Bytes(intBuffer.ToString()));
                throw new IOException("Error: Expected an integer type at offset " + reader.GetPosition(), e);
            }

            return result;
        }

        public static void ReadExpectedString(IRandomAccessRead reader, string expectedstring)
        {
            ReadExpectedString(reader, expectedstring, false);
        }

        /**
         * Reads given pattern from {@link #seqSource}. Skipping whitespace at start and end if wanted.
         * 
         * @param expectedstring pattern to be skipped
         * @param skipSpaces if set to true spaces before and after the string will be skipped
         * @throws IOException if pattern could not be read
         */
        public static void ReadExpectedString(IRandomAccessRead reader, string expectedstring, bool skipSpaces)
        {
            SkipSpaces(reader);

            foreach (var c in expectedstring)
            {
                if (reader.Read() != c)
                {
                    throw new IOException($"Expected string \'{expectedstring}\' but missed character \'{c}\' at offset {reader.GetPosition()}");
                }
            }

            SkipSpaces(reader);
        }

        public static bool IsSpace(IRandomAccessRead reader)
        {
            return IsSpace(reader.Peek());
        }

        /**
         * This will tell if the given value is a space or not.
         * 
         * @param c The character to check against space
         * @return true if the next byte in the stream is a space character.
         */
        public static bool IsSpace(int c)
        {
            return ' ' == c;
        }

        public static void ReadExpectedChar(IRandomAccessRead reader, char ec)
        {
            char c = (char)reader.Read();

            if (c != ec)
            {
                throw new InvalidOperationException($"expected=\'{ec}\' actual=\'{c}\' at offset {reader.GetPosition()}");
            }
        }

        public static bool IsHexDigit(char ch)
        {
            return char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
        }

        public static bool IsValidUtf8(byte[] input)
        {
            try
            {
                var d = Encoding.UTF8.GetDecoder();

                var charLength = d.GetCharCount(input, 0, input.Length);
                var chars = new char[charLength];
                d.Convert(input, 0, input.Length, chars, 0, charLength, true, out _, out _, out _);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

