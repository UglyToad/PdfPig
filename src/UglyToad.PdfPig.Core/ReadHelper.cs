namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Buffers.Text;
    using System.Collections.Generic;
    using System.Text;

#if NET8_0_OR_GREATER
    using System.Text.Unicode;
#endif

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

        /// <summary>
        /// The tab '\t' character.
        /// </summary>
        public const byte AsciiTab = 9;

        private static readonly HashSet<int> EndOfNameCharacters =
        [
            ' ',
            AsciiCarriageReturn,
            AsciiLineFeed,
            AsciiTab,
            '>',
            '<',
            '[',
            '/',
            ']',
            ')',
            '(',
            0,
            '\f'
        ];

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
        /// Determines if a character is whitespace or not, this includes newlines.
        /// </summary>
        /// <remarks>
        /// These values are specified in table 1 (page 12) of ISO 32000-1:2008.
        /// </remarks>
        public static bool IsWhitespace(byte c)
        {
            return c is 0 or 32 or AsciiLineFeed or AsciiCarriageReturn or 9 or 12;
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
        /// Resets to the current offset once read.
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
        /// Whether the given string is at this position in the input.
        /// Resets to the current offset once read.
        /// </summary>
        public static bool IsString(IInputBytes bytes, ReadOnlySpan<byte> s)
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

            Span<byte> buffer = stackalloc byte[19]; // max formatted uint64 length

            ReadNumberAsUtf8Bytes(bytes, buffer, out int bytesRead);

            ReadOnlySpan<byte> longBytes = buffer.Slice(0, bytesRead);

            if (Utf8Parser.TryParse(longBytes, out long result, out _))
            {
                return result;
            }
            else
            {
                bytes.Seek(bytes.CurrentOffset - bytesRead);

                throw new InvalidOperationException($"Error: Expected a long type at offset {bytes.CurrentOffset}, instead got \'{OtherEncodings.BytesAsLatin1String(longBytes)}\'");
            }
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
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            SkipSpaces(bytes);

            Span<byte> buffer = stackalloc byte[10]; // max formatted uint32 length

            ReadNumberAsUtf8Bytes(bytes, buffer, out int bytesRead);

            var intBytes = buffer.Slice(0, bytesRead);

            if (Utf8Parser.TryParse(intBytes, out int result, out _))
            {
                return result;
            }
            else
            {
                bytes.Seek(bytes.CurrentOffset - bytesRead);
                
                throw new PdfDocumentFormatException($"Error: Expected an integer type at offset {bytes.CurrentOffset}, instead got \'{OtherEncodings.BytesAsLatin1String(intBytes)}\'");
            }
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
#if NET8_0_OR_GREATER
            return char.IsAsciiHexDigit(ch);
#else
            return char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
#endif
        }

        /// <summary>
        /// Whether the given input bytes are valid UTF8.
        /// </summary>
        public static bool IsValidUtf8(byte[] input)
        {
#if NET8_0_OR_GREATER
            return Utf8.IsValid(input);
#else
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
#endif
        }

        private static void ReadNumberAsUtf8Bytes(IInputBytes reader, scoped Span<byte> buffer, out int bytesRead)
        {
            int position = 0;

            byte lastByte;
            
            while (reader.MoveNext() && (lastByte = reader.CurrentByte) != ' ' &&
                   lastByte != AsciiLineFeed &&
                   lastByte != AsciiCarriageReturn &&
                   lastByte != 60 && // see sourceforge bug 1714707
                   lastByte != '[' && // PDFBOX-1845
                   lastByte != '(' && // PDFBOX-2579
                   lastByte != 0)
            {
                if (position >= buffer.Length)
                {
                    throw new InvalidOperationException($"Number \'{OtherEncodings.BytesAsLatin1String(buffer.Slice(0, position))}\' is getting too long, stop reading at offset {reader.CurrentOffset}");
                }

                buffer[position++] = lastByte;                
            }

            if (!reader.IsAtEnd())
            {
                reader.Seek(reader.CurrentOffset - 1);
            }

            bytesRead = position;
        }
    }
}

