namespace UglyToad.PdfPig.Graphics.Operations
{
    using PdfPig.Core;
    using System;
    using System.Buffers.Text;
    using System.IO;
    using System.Text;
    using Util;

    internal static class OperationWriteHelper
    {
        private const byte Space = (byte)' ';
        private const byte NewLine = (byte)'\n';

        public static void WriteText(this Stream stream, string text, bool appendWhitespace = false)
        {
#if NET8_0_OR_GREATER
            if (Ascii.IsValid(text))
            {
                Span<byte> buffer = text.Length <= 64
                    ? stackalloc byte[text.Length]
                    : new byte[text.Length];

                Ascii.FromUtf16(text, buffer, out _);

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
                stream.WriteByte(Space);
            }
        }

        public static void WriteText(this Stream stream, ReadOnlySpan<byte> asciiBytes, bool appendWhitespace = false)
        {
            stream.Write(asciiBytes);

            if (appendWhitespace)
            {
                stream.WriteByte(Space);
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
            stream.WriteByte(Space);
        }

        public static void WriteNewLine(this Stream stream)
        {
            stream.WriteByte(NewLine);
        }

        public static void WriteDouble(this Stream stream, double value)
        {
            Span<byte> buffer = stackalloc byte[32]; // matches dotnet Number.CharStackBufferSize

            Utf8Formatter.TryFormat(value, buffer, out int bytesWritten);

            stream.Write(buffer.Slice(0, bytesWritten));
        }

        public static void WriteNumberText(this Stream stream, int number, string text)
        {
            stream.WriteDouble(number);
            stream.WriteByte(Space);
            stream.WriteText(text);
            stream.WriteNewLine();
        }

        public static void WriteNumberText(this Stream stream, int number, ReadOnlySpan<byte> asciiBytes)
        {
            stream.WriteDouble(number);
            stream.WriteByte(Space);
            stream.WriteText(asciiBytes);
            stream.WriteNewLine();
        }

        public static void WriteNumberText(this Stream stream, double number, string text)
        {
            stream.WriteDouble(number);
            stream.WriteByte(Space);
            stream.WriteText(text);
            stream.WriteNewLine();
        }
    }
}
