namespace UglyToad.PdfPig.Tests.Fonts.CompactFontFormat
{
    using PdfPig.Fonts.CompactFontFormat;

    /// <summary>
    /// <see cref="CompactFontFormatFont.TryGetGlyphIndexByName"/> exposes the charset's name -> glyph index so a
    /// consumer loading the CFF program itself (e.g. wrapped in an OpenType container) can address the glyph
    /// PdfPig draws for a name.
    /// </summary>
    public class CompactFontFormatGlyphIndexTests
    {
        private static CompactFontFormatFontCollection ParseMinionPro()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "CompactFontFormat", "MinionPro.bin");
            return CompactFontFormatParser.Parse(new CompactFontFormatData(File.ReadAllBytes(path)));
        }

        [Fact]
        public void NamedGlyphHasNonZeroIndex()
        {
            var collection = ParseMinionPro();

            Assert.True(collection.TryGetGlyphIndexByName("A", out var glyphIndex));
            Assert.True(glyphIndex > 0);
        }

        [Fact]
        public void IndexRoundTripsThroughCharsetName()
        {
            var font = ParseMinionPro().FirstFont;

            Assert.True(font.TryGetGlyphIndexByName("B", out var glyphIndex));
            Assert.Equal("B", font.Charset.GetNameByGlyphId(glyphIndex));
        }

        [Fact]
        public void NotDefResolvesToGlyphZero()
        {
            var font = ParseMinionPro().FirstFont;

            Assert.True(font.TryGetGlyphIndexByName(".notdef", out var glyphIndex));
            Assert.Equal(0, glyphIndex);
        }

        [Fact]
        public void UnknownNameIsNotFound()
        {
            var font = ParseMinionPro().FirstFont;

            Assert.False(font.TryGetGlyphIndexByName("definitely.not.a.glyph", out var glyphIndex));
            Assert.Equal(0, glyphIndex);
        }
    }
}