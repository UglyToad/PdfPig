namespace UglyToad.PdfPig.Tests.Fonts.Encodings
{
    using System.Text;
    using PdfPig.Fonts;

    public class GlyphListTests
    {
        [Fact]
        public void CanLoadAdobeGlyphList()
        {
            var list = GlyphList.AdobeGlyphList;

            var result = list.NameToUnicode("Acute");

            Assert.Equal("\uF6C9", result);
        }

        [Fact]
        public void CanLoadZapfDingbatsGlyphList()
        {
            var list = GlyphList.ZapfDingbats;

            var result = list.NameToUnicode("a69");

            Assert.Equal("\u274A", result);
        }

        [Fact]
        public void UnicodeToNameWorks()
        {
            var result = GlyphList.AdobeGlyphList.UnicodeCodePointToName(79);

            Assert.Equal("O", result);
        }

        [Fact]
        public void UnicodeToNameNotDefined()
        {
            var list = new GlyphList(new Dictionary<string, string>());

            var result = list.UnicodeCodePointToName(120);

            Assert.Equal(".notdef", result);
        }

        [Fact]
        public void NameToUnicodeNull()
        {
            var list = new GlyphList(new Dictionary<string, string>());

            var result = list.NameToUnicode(null);

            Assert.Null(result);
        }

        [Fact]
        public void NameToUnicodeRemovesSuffix()
        {
            var list = new GlyphList(new Dictionary<string, string>
            {
                { "Boris", "B" }
            });

            var result = list.NameToUnicode("Boris.Special");

            Assert.Equal("B", result);
        }

        [Fact]
        public void NameToUnicodeConvertsHexAndUsesHexValue()
        {
            var list = new GlyphList(new Dictionary<string, string>
            {
                { "B", "X" }
            });

            var result = list.NameToUnicode("uni0042");

            Assert.Equal("B", result);
        }

        [Fact]
        public void NameToUnicodeConvertsShortHexAndUsesHexValue()
        {
            var list = new GlyphList(new Dictionary<string, string>
            {
                { "E", "Æ" }
            });

            var result = list.NameToUnicode("u0045");

            Assert.Equal("E", result);
        }


        [Fact]
        public void NameToUnicodeConvertAglSpecification()
        {
            // https://github.com/adobe-type-tools/agl-specification?tab=readme-ov-file#3-examples
            var list = new GlyphList(new Dictionary<string, string>
            {
                { "Lcommaaccent", "\u013B" }
            });

            var result = list.NameToUnicode("Lcommaaccent_uni20AC0308_u1040C.alternate");

            // This value is not encodable in single characters in UTF-16, so we get a surrogate pair for the final unicode character
            Assert.Equal("\u013B\u20AC\u0308\uD801\uDC0C", result);

#if NET6_0_OR_GREATER
            // But in .Net we can get the unicode rune values to verify that this is really the expected value
            var runes = result.EnumerateRunes().ToList();

            Assert.Equal(4, runes.Count);

            Assert.Equal(0x013B, runes[0].Value);
            Assert.Equal(0x20AC, runes[1].Value);
            Assert.Equal(0x0308, runes[2].Value);
            Assert.Equal(0x1040C, runes[3].Value);
#endif
            // Ok, so we know this is what the actual string is. Now lets encode that last value the C# way
            Assert.Equal("\u013B\u20AC\u0308\U0001040C", result);
        }
    }
}
