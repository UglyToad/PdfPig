namespace UglyToad.Pdf.Tokenization.Tokens
{
    using System.Collections.Generic;
    using System.Text;

    public class HexToken : IDataToken<string>
    {
        private static readonly Dictionary<char, byte> HexMap = new Dictionary<char, byte>
        {
            {'0', 0x00 },
            {'1', 0x01 },
            {'2', 0x02 },
            {'3', 0x03 },
            {'4', 0x04 },
            {'5', 0x05 },
            {'6', 0x06 },
            {'7', 0x07 },
            {'8', 0x08 },
            {'9', 0x09 },

            {'A', 0x0A },
            {'a', 0x0A },
            {'B', 0x0B },
            {'b', 0x0B },
            {'C', 0x0C },
            {'c', 0x0C },
            {'D', 0x0D },
            {'d', 0x0D },
            {'E', 0x0E },
            {'e', 0x0E },
            {'F', 0x0F },
            {'f', 0x0F }
        };

        private static byte Convert(char high, char low)
        {
            var highByte = HexMap[high];
            var lowByte = HexMap[low];

            return (byte)(highByte << 4 | lowByte);
        }

        public string Data { get; }

        public IReadOnlyList<byte> Bytes { get; }

        public HexToken(IReadOnlyList<char> characters)
        {
            var bytes = new List<byte>();
            var builder = new StringBuilder();

            for (int i = 0; i < characters.Count; i += 2)
            {
                char high = characters[i];
                char low;
                if (i == characters.Count - 1)
                {
                    low = '0';
                }
                else
                {
                    low = characters[i + 1];
                }

                var b = Convert(high, low);
                bytes.Add(b);
                builder.Append((char)b);
            }

            Bytes = bytes;
            Data = builder.ToString();
        }
    }
}