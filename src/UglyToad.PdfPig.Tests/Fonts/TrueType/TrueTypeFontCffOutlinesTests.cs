namespace UglyToad.PdfPig.Tests.Fonts.TrueType
{
    using PdfPig.Core;
    using PdfPig.Fonts.CompactFontFormat;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using static UglyToad.PdfPig.Tests.Fonts.TestFontPrograms;

    /// <summary>
    /// An OpenType font with PostScript outlines (CFF table, no glyf) resolves character codes to glyphs like a
    /// TrueType one: through the supplied mapping, else the cmap.
    /// </summary>
    public class TrueTypeFontCffOutlinesTests
    {
        private static readonly CompactFontFormatFont MinionProCff = CompactFontFormatParser.Parse(new CompactFontFormatData(MinionPro)).FirstFont;

        [Fact]
        public void CodeIsLookedUpInTheCmap()
        {
            // The cmap maps 'Z' to MinionPro's /A; 'Z' (90) as a glyph index would be another glyph.
            var font = Parse(OpenTypeCffFont('Z'));

            Assert.True(font.TryGetPath('Z', out var path));
            Assert.Equal(GetBounds("A"), PdfSubpath.GetBoundingRectangle(path));

            Assert.True(font.TryGetBoundingBox('Z', out var boundingBox));
            Assert.Equal(MinionProCff.GetCharacterBoundingBox("A"), boundingBox);
        }

        [Fact]
        public void CodeIsLookedUpInASymbolCmap()
        {
            // A (3,0) cmap at U+F05A: the cmap lookup also tries the code moved to U+F0xx, as for glyf outlines.
            var font = Parse(OpenTypeCffFont(0xF000 + 'Z', unicode: false));

            Assert.True(font.TryGetPath('Z', out var path));
            Assert.Equal(GetBounds("A"), PdfSubpath.GetBoundingRectangle(path));
        }

        [Fact]
        public void CodeMissingFromTheCmapHasNoGlyph()
        {
            var font = Parse(OpenTypeCffFont('Z'));

            Assert.False(font.TryGetPath('Q', out _));
            Assert.False(font.TryGetBoundingBox('Q', out _));
        }

        [Fact]
        public void SuppliedMappingIsUsedBeforeTheCmap()
        {
            Assert.True(MinionProCff.TryGetGlyphIndexByName("B", out int glyphB));
            var font = Parse(OpenTypeCffFont('Z'));

            Assert.True(font.TryGetPath('Z', _ => glyphB, out var path));
            Assert.Equal(GetBounds("B"), PdfSubpath.GetBoundingRectangle(path));
        }

        [Fact]
        public void MappingThatDoesNotResolveFallsBackToTheCmap()
        {
            var font = Parse(OpenTypeCffFont('Z'));

            Assert.True(font.TryGetPath('Z', _ => null, out var path));
            Assert.Equal(GetBounds("A"), PdfSubpath.GetBoundingRectangle(path));
        }

        [Fact]
        public void WithoutACmapTheCodeIsTheGlyphIndex()
        {
            // As for an embedded program addressed by glyph index: the cmap entry is renamed so the font has none.
            Assert.True(MinionProCff.TryGetGlyphIndexByName("A", out int glyphA));
            var font = Parse(ReplaceTable(OpenTypeCffFont('Z'), "cmap", new byte[4], "zcmp"));

            Assert.Null(font.TableRegister.CMapTable);
            Assert.True(font.TryGetPath(glyphA, out var path));
            Assert.Equal(GetBounds("A"), PdfSubpath.GetBoundingRectangle(path));
        }

        private static TrueTypeFont Parse(byte[] bytes) => TrueTypeFontParser.Parse(new TrueTypeDataBytes(bytes));

        private static PdfRectangle? GetBounds(string name) => PdfSubpath.GetBoundingRectangle(MinionProCff.GetCharacterPath(name));
    }
}
