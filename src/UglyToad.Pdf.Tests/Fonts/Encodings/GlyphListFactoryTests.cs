namespace UglyToad.Pdf.Tests.Fonts.Encodings
{
    using Pdf.Fonts.Encodings;
    using Xunit;

    public class GlyphListFactoryTests
    {
        [Fact]
        public void CanGetAdobeGlyphList()
        {
            var result = GlyphListFactory.Get("glyphlist");

            var h = result.NameToUnicode("H");

            Assert.Equal("H", h);
        }
    }
}
