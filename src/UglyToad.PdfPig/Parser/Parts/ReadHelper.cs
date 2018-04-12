namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Exceptions;
    using IO;
    using Util;

    internal static class ReadHelper
    {
        public const byte AsciiLineFeed = 10;
        public const byte AsciiCarriageReturn = 13;

        public static string ReadLine(IInputBytes bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.IsAtEnd())
            {
                throw new InvalidOperationException("Error: End-of-File, expected line");
            }

            var buffer = new StringBuilder(11);

            byte c = 0;
            while (bytes.MoveNext())
            {
                c = bytes.CurrentByte;

                // CR and LF are valid EOLs
                if (IsEndOfLine(c))
                {
                    break;
                }

                buffer.Append((char)c);
            }

            // CR+LF is also a valid EOL 
            if (IsCarriageReturn(c) && IsLineFeed(bytes.Peek()))
            {
                bytes.MoveNext();
            }

            return buffer.ToString();
        }
        
        public static void SkipSpaces(IInputBytes bytes)
        {
            const int commentCharacter = 37;
            bytes.MoveNext();
            byte c = bytes.CurrentByte;

            while (IsWhitespace(c) || c == 37)
            {
                if (c == commentCharacter)
                {
                    // skip past the comment section
                    bytes.MoveNext();
                    c = bytes.CurrentByte;
                    while (!IsEndOfLine(c))
                    {
                        bytes.MoveNext();
                        c = bytes.CurrentByte;
                    }
                }
                else
                {
                    bytes.MoveNext();
                    c = bytes.CurrentByte;
                }
            }

            if (!bytes.IsAtEnd())
            {
                bytes.Seek(bytes.CurrentOffset - 1);
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

        public static bool IsEndOfLine(char c) => IsEndOfLine((byte) c);
        public static bool IsEndOfLine(byte b)
        {
            return IsLineFeed(b) || IsCarriageReturn(b);
        }

        public static bool IsLineFeed(byte? c)
        {
            return AsciiLineFeed == c;
        }

        public static bool IsCarriageReturn(byte c)
        {
            return AsciiCarriageReturn == c;
        }

        public static bool IsString(IInputBytes bytes, string s)
        {
            bool found = true;

            var startOffset = bytes.CurrentOffset;

            foreach (var c in s)
            {
                if (bytes.CurrentByte != c)
                {
                    found = false;
                    break;
                }

                bytes.MoveNext();
            }

            bytes.Seek(startOffset);

            return found;
        }
        
        public static long ReadLong(IInputBytes bytes)
        {
            SkipSpaces(bytes);
            long retval;

            StringBuilder longBuffer = ReadStringNumber(bytes);

            try
            {
                retval = long.Parse(longBuffer.ToString());
            }
            catch (FormatException e)
            {
                var bytesToReverse = OtherEncodings.StringAsLatin1Bytes(longBuffer.ToString());
                bytes.Seek(bytes.CurrentOffset - bytesToReverse.Length);

                throw new InvalidOperationException($"Error: Expected a long type at offset {bytes.CurrentOffset}, instead got \'{longBuffer}\'", e);
            }

            return retval;
        }

        private static readonly int MaximumNumberStringLength = long.MaxValue.ToString("D").Length;

        private static StringBuilder ReadStringNumber(IInputBytes reader)
        {
            byte lastByte;
            StringBuilder buffer = new StringBuilder();

            while (reader.MoveNext() && (lastByte = reader.CurrentByte) != ' ' &&
                   lastByte != AsciiLineFeed &&
                   lastByte != AsciiCarriageReturn &&
                   lastByte != 60 && //see sourceforge bug 1714707
                   lastByte != '[' && // PDFBOX-1845
                   lastByte != '(' && // PDFBOX-2579
                   lastByte != 0)
            {
                buffer.Append((char)lastByte);

                if (buffer.Length > MaximumNumberStringLength)
                {
                    throw new InvalidOperationException($"Number \'{buffer}\' is getting too long, stop reading at offset {reader.CurrentOffset}");
                }
            }

            if (!reader.IsAtEnd())
            {
                reader.Seek(reader.CurrentOffset - 1);
            }

            return buffer;
        }
        
        /// <summary>
        /// This will tell if the given value is a digit or not.
        /// </summary>
        public static bool IsDigit(int c)
        {
            return c >= '0' && c <= '9';
        }

        public static int ReadInt(IInputBytes bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            SkipSpaces(bytes);
            int result;

            var intBuffer = ReadStringNumber(bytes);

            try
            {
                result = int.Parse(intBuffer.ToString());
            }
            catch (Exception e)
            {
                bytes.Seek(bytes.CurrentOffset - OtherEncodings.StringAsLatin1Bytes(intBuffer.ToString()).Length);

                throw new PdfDocumentFormatException($"Error: Expected an integer type at offset {bytes.CurrentOffset}", e);
            }

            return result;
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

        public static bool IsHex(byte b) => IsHex((char) b);
        public static bool IsHex(char ch)
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

