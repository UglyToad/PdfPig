namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.IO;
    using Tokenization.Tokens;

    internal class AsciiHexDecodeFilter : IFilter
    {
        private static readonly short[] ReverseHex = 
        {
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
        };

        public byte[] Decode(byte[] input, DictionaryToken streamDictionary, int filterIndex)
        {
            var pair = new byte[2];
            var index = 0;

            using (var memoryStream = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(memoryStream))
            {
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
                        WriteHexToByte(pair, binaryWriter);

                        index = 0;
                    }
                }

                if (index > 0)
                {
                    if (index == 1)
                    {
                        pair[1] = (byte) '0';
                    }

                    WriteHexToByte(pair, binaryWriter);
                }

                binaryWriter.Flush();
                return memoryStream.ToArray();
            }
        }

        private static void WriteHexToByte(byte[] hexBytes, BinaryWriter writer)
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
