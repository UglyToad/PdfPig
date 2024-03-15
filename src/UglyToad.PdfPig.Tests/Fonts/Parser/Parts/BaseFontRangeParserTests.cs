namespace UglyToad.PdfPig.Tests.Fonts.Parser.Parts
{
    using PdfFonts.Cmap;
    using PdfFonts.Parser.Parts;
    using PdfPig.Tokens;

    public class BaseFontRangeParserTests
    {
        private readonly BaseFontRangeParser parser = new BaseFontRangeParser();

        [Fact]
        public void CanParseWithArray()
        {
            var input = StringBytesTestConverter.Scanner("<0003> <0004> [<0020> <0041>]");

            var builder = new CharacterMapBuilder();

            parser.Parse(new NumericToken(1), input.scanner, builder);

            Assert.Equal(2, builder.BaseFontCharacterMap.Count);

            Assert.Equal(" ", builder.BaseFontCharacterMap[3]);
            Assert.Equal("A", builder.BaseFontCharacterMap[4]);
        }

        [Fact]
        public void CanParseWithHex()
        {
            var input = StringBytesTestConverter.Scanner("<8141> <8147> <8141>");

            var builder = new CharacterMapBuilder();

            parser.Parse(new NumericToken(1), input.scanner, builder);

            Assert.Equal(7, builder.BaseFontCharacterMap.Count);

            Assert.Equal("腁", builder.BaseFontCharacterMap[33089]);
            Assert.Equal(char.ConvertFromUtf32(33090), builder.BaseFontCharacterMap[33090]);
        }

        [Fact]
        public void CanParseTwoRowsWithDifferentFormat()
        {
            var input = StringBytesTestConverter.Scanner(@"<0019> <001B> <3C>
<0001> <0003> [/happy /feet /penguin]");

            var builder = new CharacterMapBuilder();

            parser.Parse(new NumericToken(2), input.scanner, builder);

            Assert.Equal(6, builder.BaseFontCharacterMap.Count);

            Assert.Equal("happy", builder.BaseFontCharacterMap[1]);
            Assert.Equal("feet", builder.BaseFontCharacterMap[2]);
            Assert.Equal("penguin", builder.BaseFontCharacterMap[3]);

            Assert.Equal("<", builder.BaseFontCharacterMap[25]);
        }
    }
}
