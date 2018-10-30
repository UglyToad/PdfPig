namespace UglyToad.PdfPig.Fonts.Type1.CharStrings
{
    using System.Collections.Generic;
    using Parser;

    internal class Type1CharStringParser
    {
        public static void Parse(IReadOnlyList<Type1CharstringDecryptedBytes> charStrings, IReadOnlyList<Type1CharstringDecryptedBytes> subroutines)
        {
            foreach (var charString in charStrings)
            {
                ParseSingle(charString.Bytes);
            }
        }

        private static void ParseSingle(IReadOnlyList<byte> charStringBytes)
        {
            var numberStack = new List<int>();
            for (var i = 0; i < charStringBytes.Count; i++)
            {
                var b = charStringBytes[i];

                if (b <= 31)
                {
                    var command = Type1CharStringCommandFactory.GetCommand(numberStack, b, charStringBytes, ref i);
                }
                else
                {
                    var val = InterpretNumber(b, charStringBytes, ref i);

                    numberStack.Add(val);
                }
            }
        }

        private static int InterpretNumber(byte b, IReadOnlyList<byte> bytes, ref int i)
        {
            if (b >= 32 && b <= 246)
            {
                return b - 139;
            }

            if (b >= 247 && b <= 250)
            {
                var w = bytes[++i];
                return ((b - 247) * 256) + w + 108;
            }

            if (b >= 251 && b <= 254)
            {
                var w = bytes[++i];
                return -((b - 251) * 256) - w - 108;
            }

            var result = bytes[++i] << 24 + bytes[++i] << 16 + bytes[++i] << 8 + bytes[++i];

            return result;
        }
    }
}
