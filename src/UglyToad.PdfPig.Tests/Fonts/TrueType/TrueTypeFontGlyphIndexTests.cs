namespace UglyToad.PdfPig.Tests.Fonts.TrueType
{
    using PdfPig.Core;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;

    /// <summary>
    /// <see cref="TrueTypeFont.TryGetGlyphIndex(int, out int)"/> exposes the same code -> glyph index resolution
    /// <see cref="TrueTypeFont.TryGetPath(int, out System.Collections.Generic.IReadOnlyList{Core.PdfSubpath})"/> uses, so a
    /// renderer that loads the font program itself draws exactly the outline PdfPig would.
    /// </summary>
    public class TrueTypeFontGlyphIndexTests
    {
        private static TrueTypeFont ParseRoboto()
        {
            var bytes = TrueTypeTestHelper.GetFileBytes("Roboto-Regular");
            return TrueTypeFontParser.Parse(new TrueTypeDataBytes(new MemoryInputBytes(bytes)));
        }

        [Fact]
        public void GlyphIndexMatchesCmapLookup()
        {
            var font = ParseRoboto();

            Assert.True(font.TryGetGlyphIndex('A', out var glyphIndex));
            Assert.True(glyphIndex > 0);

            Assert.True(font.TableRegister.CMapTable.TryGetGlyphIndex('A', out var cmapGlyphIndex));
            Assert.Equal(cmapGlyphIndex, glyphIndex);
        }

        [Fact]
        public void GlyphIndexAddressesTheSameOutlineAsTryGetPath()
        {
            var font = ParseRoboto();

            Assert.True(font.TryGetGlyphIndex('g', out var glyphIndex));
            Assert.True(font.TryGetPath('g', out var pathByCode));
            Assert.True(font.TableRegister.GlyphTable.TryGetGlyphPath(glyphIndex, out var pathByIndex));

            Assert.Equal(pathByCode.Count, pathByIndex.Count);
            for (var i = 0; i < pathByCode.Count; i++)
            {
                Assert.Equal(pathByCode[i].Commands.Count, pathByIndex[i].Commands.Count);
            }
        }

        [Fact]
        public void ExternalMappingTakesPrecedenceOverCmap()
        {
            var font = ParseRoboto();

            // A CIDToGIDMap or the simple-font encoding rules supply the index directly; the cmap is not consulted.
            Assert.True(font.TryGetGlyphIndex('A', code => 42, out var glyphIndex));
            Assert.Equal(42, glyphIndex);

            // Returning null from the mapping falls back to the cmap.
            Assert.True(font.TryGetGlyphIndex('A', code => null, out var fallbackGlyphIndex));
            Assert.True(font.TableRegister.CMapTable.TryGetGlyphIndex('A', out var cmapGlyphIndex));
            Assert.Equal(cmapGlyphIndex, fallbackGlyphIndex);
        }

        [Fact]
        public void UnmappedCodeReportsNoGlyph()
        {
            var font = ParseRoboto();

            // U+E000 (private use) is not in Roboto's cmap.
            var found = font.TryGetGlyphIndex(0xE000, out var glyphIndex);

            Assert.False(found && glyphIndex != 0);
        }
    }
}