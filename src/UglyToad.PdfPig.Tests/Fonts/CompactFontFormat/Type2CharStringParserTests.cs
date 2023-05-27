namespace UglyToad.PdfPig.Tests.Fonts.CompactFontFormat
{
    using PdfPig.Fonts.CompactFontFormat;
    using PdfPig.Fonts.CompactFontFormat.Charsets;
    using PdfPig.Fonts.CompactFontFormat.CharStrings;
    using System;
    using Xunit;

    public class Type2CharStringParserTests
    {
        private const string Tika2121LegacySerifBookLetterBHex =
            "F7 1A 8A A9 F7 BC A8 F7 93 A6 12 F7 13 DA F7 67 EA 63 E8 13 F8 F8 94 F8 84 15 D6 55 AE 6F 97 1E " +
            "98 6E 67 90 47 1B 6E 3C 89 6C 67 56 79 0C 22 85 88 8A 7E 81 8E 8A 98 C2 96 7E 31 8C 1F 8C 3F 8C " +
            "73 70 1A 5D 07 49 8B FB 10 85 4E 1E 86 5B 7A 82 64 88 08 77 89 89 8B 81 1A 7E 8F 8B 9B 1E 97 A8 " +
            "8D CC BC CA 9F 0C 22 13 F4 E3 BB 95 AC BE 1F BB A9 AD C5 C4 1A A6 82 CA 49 B2 1E 5A A7 5A 8E 6C " +
            "8D 08 13 F8 BE 9A EF A7 F3 1A FB C6 FB 22 15 F7 2C 8B DF 8F 94 1E 95 90 A5 8B 9C 1B BD F7 01 8B " +
            "FB 12 22 44 73 52 1F 13 F4 F7 4B FB 42 15 FB 26 FB 1E 86 5C 54 79 96 B0 86 1E 86 AB 8C F7 3D 8C " +
            "BA 08 F7 10 8C F7 22 8C FB 27 1A 0E";

        private static readonly CompactFontFormatSubroutinesSelector EmptySubroutinesSelector =
            new CompactFontFormatSubroutinesSelector(null, null);

        [Fact]
        public void ParsesTika2121LegacySerifBookFontLetterBCorrectly()
        {
            var input = StringToByteArray(Tika2121LegacySerifBookLetterBHex);

            var result = Type2CharStringParser.Parse(
                new[] { input },
                EmptySubroutinesSelector,
                NameCharset.Instance);

            var sequence = Assert.Single(result.CharStrings).Value;
        }

        public static byte[] StringToByteArray(string hex)
        {
            hex = hex.Replace(" ", string.Empty);

            var result = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return result;
        }

        private class NameCharset : ICompactFontFormatCharset
        {
            public static readonly NameCharset Instance = new NameCharset();

            public bool IsCidCharset { get; } = false;

            private NameCharset()
            {
            }

            public string GetNameByGlyphId(int glyphId)
                => "A";

            public string GetNameByStringId(int stringId)
                => "A";

            public int GetStringIdByGlyphId(int glyphId)
                => 1;

            public int GetGlyphIdByName(string characterName)
                => 1;
        }
    }
}
