namespace UglyToad.PdfPig.Tests.Fonts.Encodings
{
    using PdfPig.Fonts.Encodings;
    using Xunit;

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
    }
}
