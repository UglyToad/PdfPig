namespace UglyToad.PdfPig.Tests.Fonts.Encodings
{
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


        [Fact(Skip = "TODO - String don't match")]
        public void NameToUnicodeConvertAglSpecification()
        {
            // https://github.com/adobe-type-tools/agl-specification?tab=readme-ov-file#3-examples
            var list = new GlyphList(new Dictionary<string, string>
            {
                { "Lcommaaccent", "\u013B" }
            });

            var result = list.NameToUnicode("Lcommaaccent_uni20AC0308_u1040C.alternate");

            Assert.Equal("\u013B\u20AC\u0308\u1040C", result);
        }
    }
}
