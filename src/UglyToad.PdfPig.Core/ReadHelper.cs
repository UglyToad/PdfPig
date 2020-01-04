namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Helper methods for reading from PDF files.
    /// </summary>
    public static class ReadHelper
    {
        /// <summary>
        /// The line-feed '\n' character.
        /// </summary>
        public const byte AsciiLineFeed = 10;

        /// <summary>
        /// The carriage return '\r' character.
        /// </summary>
        public const byte AsciiCarriageReturn = 13;

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

        private static readonly int MaximumNumberStringLength = long.MaxValue.ToString("D").Length;

        /// <summary>
        /// Read a string from the input until a newline.
        /// </summary>
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
        
        /// <summary>
        /// Skip any whitespace characters.
        /// </summary>
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
        
        /// <summary>
        /// Whether the given character value is the end of a PDF Name token.
        /// </summary>
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
        public static bool IsWhitespace(byte c)
        {
            return c == 0 || c == 32 || c == AsciiLineFeed || c == AsciiCarriageReturn || c == 9 || c == 12;
        }

        /// <summary>
        /// Whether the character is an end of line character.
        /// </summary>
        public static bool IsEndOfLine(char c) => IsEndOfLine((byte) c);

        /// <summary>
        /// Whether the character is an end of line character.
        /// </summary>
        public static bool IsEndOfLine(byte b)
        {
            return IsLineFeed(b) || IsCarriageReturn(b);
        }

        /// <summary>
        /// Whether the character is an line feed '\n' character.
        /// </summary>
        public static bool IsLineFeed(byte? c)
        {
            return AsciiLineFeed == c;
        }

        /// <summary>
        /// Whether the character is a carriage return '\r' character.
        /// </summary>
        public static bool IsCarriageReturn(byte c)
        {
            return AsciiCarriageReturn == c;
        }

        /// <summary>
        /// Whether the given string is at this position in the input.
        /// </summary>
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
        
        /// <summary>
        /// Read a long from the input.
        /// </summary>
        public static long ReadLong(IInputBytes bytes)
        {
            SkipSpaces(bytes);
            long retval;

            StringBuilder longBuffer = ReadStringNumber(bytes);

            try
            {
                retval = long.Parse(longBuffer.ToString(), CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                var bytesToReverse = OtherEncodings.StringAsLatin1Bytes(longBuffer.ToString());
                bytes.Seek(bytes.CurrentOffset - bytesToReverse.Length);

                throw new InvalidOperationException($"Error: Expected a long type at offset {bytes.CurrentOffset}, instead got \'{longBuffer}\'", e);
            }

            return retval;
        }

        
        /// <summary>
        /// Whether the given value is a digit or not.
        /// </summary>
        public static bool IsDigit(int c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Read an int from the input.
        /// </summary>
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
                result = int.Parse(intBuffer.ToString(), CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                bytes.Seek(bytes.CurrentOffset - OtherEncodings.StringAsLatin1Bytes(intBuffer.ToString()).Length);

                throw new PdfDocumentFormatException($"Error: Expected an integer type at offset {bytes.CurrentOffset}", e);
            }

            return result;
        }
        
        /// <summary>
        /// Whether the given character is a space.
        /// </summary>
        public static bool IsSpace(int c)
        {
            return c == ' ';
        }

        /// <summary>
        /// Whether the given character value is a valid hex value.
        /// </summary>
        public static bool IsHex(byte b) => IsHex((char) b);

        /// <summary>
        /// Whether the given character value is a valid hex value.
        /// </summary>
        public static bool IsHex(char ch)
        {
            return char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
        }

        /// <summary>
        /// Whether the given input bytes are valid UTF8.
        /// </summary>
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
    }
}

