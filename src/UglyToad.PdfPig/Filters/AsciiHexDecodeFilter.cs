namespace UglyToad.PdfPig.Filters
{
    using System;
    using Core;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// Encodes/decodes data using the ASCII hexadecimal encoding where each byte is represented by two ASCII characters.
    /// </summary>
    internal sealed class AsciiHexDecodeFilter : IFilter
    {
        private static readonly short[] ReverseHex = 
        [
            /*   0 */  -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            /*  10 */  -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            /*  20 */  -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            /*  30 */  -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            /*  40 */  -1, -1, -1, -1, -1, -1, -1, -1,  0,  1,
            /*  50 */   2,  3,  4,  5,  6,  7,  8,  9, -1, -1,
            /*  60 */  -1, -1, -1, -1, -1, 10, 11, 12, 13, 14,
            /*  70 */  15, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            /*  80 */  -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            /*  90 */  -1, -1, -1, -1, -1, -1, -1, 10, 11, 12,
            /* 100 */  13, 14, 15
        ];

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            Span<byte> pair = stackalloc byte[2];
            var index = 0;

            using var writer = new ArrayPoolBufferWriter<byte>(input.Length);

            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '>')
                {
                    break;
                }

                if (IsWhitespace(input[i]) || input[i] == '<')
                {
                    continue;
                }

                pair[index] = input[i];
                index++;

                if (index == 2)
                {
                    WriteHexToByte(pair, writer);

                    index = 0;
                }
            }

            if (index > 0)
            {
                if (index == 1)
                {
                    pair[1] = (byte)'0';
                }

                WriteHexToByte(pair, writer);
            }

            return writer.WrittenSpan.ToArray();
        }

        private static void WriteHexToByte(ReadOnlySpan<byte> hexBytes, ArrayPoolBufferWriter<byte> writer)
        {
            var first = ReverseHex[hexBytes[0]];
            var second = ReverseHex[hexBytes[1]];

            if (first == -1)
            {
                throw new InvalidOperationException("Invalid character encountered in hex encoded stream: " + (char)hexBytes[0]);
            }

            if (second == -1)
            {
                throw new InvalidOperationException("Invalid character encountered in hex encoded stream: " + (char)hexBytes[0]);
            }

            var value = (byte) (first * 16 + second);

            writer.Write(value);
        }

        private static bool IsWhitespace(byte c)
        {
            return c == 0 || c == '\t' || c == '\n' || c == '\f' || c == '\r' || c == ' ';
        }
    }
}
