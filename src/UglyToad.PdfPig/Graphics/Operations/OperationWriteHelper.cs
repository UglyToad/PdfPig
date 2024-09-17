namespace UglyToad.PdfPig.Graphics.Operations
{
    using PdfPig.Core;
    using System;
    using System.Buffers;
    using System.Buffers.Text;
    using System.Globalization;
    using System.IO;
    using Util;

    internal static class OperationWriteHelper
    {
        private const byte Whitespace = (byte)' ';
        private const byte NewLine = (byte)'\n';
        private const byte Zero = (byte)'0';
        private const byte Point = (byte)'.';

        private static readonly StandardFormat StandardFormatDouble = new StandardFormat('F', 9);

        public static void WriteText(this Stream stream, string text, bool appendWhitespace = false)
        {
#if NET8_0_OR_GREATER
            if (System.Text.Ascii.IsValid(text))
            {
                Span<byte> buffer = text.Length <= 64
                    ? stackalloc byte[text.Length]
                    : new byte[text.Length];

                System.Text.Ascii.FromUtf16(text, buffer, out _);

                stream.Write(buffer);
            }
            else
            {
                var bytes = OtherEncodings.StringAsLatin1Bytes(text);
                stream.Write(bytes);
            }
#else
            var bytes = OtherEncodings.StringAsLatin1Bytes(text);
            stream.Write(bytes, 0, bytes.Length);
#endif
            if (appendWhitespace)
            {
                stream.WriteByte(Whitespace);
            }
        }

        public static void WriteText(this Stream stream, ReadOnlySpan<byte> asciiBytes, bool appendWhitespace = false)
        {
            stream.Write(asciiBytes);

            if (appendWhitespace)
            {
                stream.WriteByte(Whitespace);
            }
        }

        public static void WriteHex(this Stream stream, ReadOnlySpan<byte> bytes)
        {
            Span<byte> hex = bytes.Length <= 64
                ? stackalloc byte[bytes.Length * 2]
                : new byte[bytes.Length * 2];

            Hex.GetUtf8Chars(bytes, hex);

            stream.WriteByte((byte)'<');
            stream.Write(hex);
            stream.WriteByte((byte)'>');
        }

        public static void WriteWhiteSpace(this Stream stream)
        {
            stream.WriteByte(Whitespace);
        }

        public static void WriteNewLine(this Stream stream)
        {
            stream.WriteByte(NewLine);
        }

        public static void WriteDouble(this Stream stream, double value)
        {
            int stackSize = 32; // matches dotnet Number.CharStackBufferSize

            bool success = TryWriteDouble(stream, value, stackSize);
            while (!success && stackSize <= 1024)
            {
                stackSize *= 2;
                success = TryWriteDouble(stream, value, stackSize);
            }

            if (!success)
            {
                ReadOnlySpan<byte> buffer = System.Text.Encoding.UTF8.GetBytes(value.ToString("F9", CultureInfo.InvariantCulture));
                int lastIndex = GetLastSignificantDigitIndex(buffer, buffer.Length);
                stream.Write(buffer.Slice(0, lastIndex));
            }
        }

        private static bool TryWriteDouble(Stream stream, double value, int stackSize)
        {
            System.Diagnostics.Debug.Assert(stackSize <= 1024);

            Span<byte> buffer = stackalloc byte[stackSize];

            if (Utf8Formatter.TryFormat(value, buffer, out int bytesWritten, StandardFormatDouble))
            {
                int lastIndex = GetLastSignificantDigitIndex(buffer, bytesWritten);
                stream.Write(buffer.Slice(0, lastIndex));
                return true;
            }

            return false;
        }

        private static int GetLastSignificantDigitIndex(ReadOnlySpan<byte> buffer, int bytesWritten)
        {
            int lastIndex = bytesWritten;
            for (int i = bytesWritten - 1; i > 1; --i)
            {
                if (buffer[i] != Zero)
                {
                    break;
                }
                lastIndex--;
            }

            if (buffer[lastIndex - 1] == Point)
            {
                lastIndex--;
            }

            return lastIndex;
        }

        public static void WriteNumberText(this Stream stream, int number, string text)
        {
            stream.WriteDouble(number);
            stream.WriteByte(Whitespace);
            stream.WriteText(text);
            stream.WriteNewLine();
        }

        public static void WriteNumberText(this Stream stream, int number, ReadOnlySpan<byte> asciiBytes)
        {
            stream.WriteDouble(number);
            stream.WriteByte(Whitespace);
            stream.WriteText(asciiBytes);
            stream.WriteNewLine();
        }

        public static void WriteNumberText(this Stream stream, double number, string text)
        {
            stream.WriteDouble(number);
            stream.WriteByte(Whitespace);
            stream.WriteText(text);
            stream.WriteNewLine();
        }
    }
}
