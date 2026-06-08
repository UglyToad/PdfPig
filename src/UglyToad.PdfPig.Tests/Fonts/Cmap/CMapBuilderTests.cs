namespace UglyToad.PdfPig.Tests.Fonts.Cmap
{
    using PdfFonts.Cmap;

    public class CMapBuilderTests
    {
        [Fact]
        public void ShorterCodeWinsOverLongerCollidingCode()
        {
            var builder = new CharacterMapBuilder();

            // Longer code added first (as it appears in the document), then the shorter code.
            builder.AddBaseFontCharacter(new byte[] { 0x00, 0x20 }, "X");
            builder.AddBaseFontCharacter(new byte[] { 0x20 }, " ");

            Assert.Equal(" ", builder.BaseFontCharacterMap[32]);
        }

        [Fact]
        public void ShorterCodeWinsRegardlessOfOrder()
        {
            var builder = new CharacterMapBuilder();

            // Shorter code added first, the colliding longer code must not overwrite it.
            builder.AddBaseFontCharacter(new byte[] { 0x20 }, " ");
            builder.AddBaseFontCharacter(new byte[] { 0x00, 0x20 }, "X");

            Assert.Equal(" ", builder.BaseFontCharacterMap[32]);
        }

        // When a zero-padded code is defined more than once, the first mapping encountered takes
        // precedence - a zero-padded code never overwrites an existing entry (see GitHub issue #1309 -
        // LKR824191.pdf, where the 2-byte <0003> maps to a regular space first and a non-breaking
        // space later).
        [Fact]
        public void FirstMappingWinsForZeroPaddedDuplicateCode()
        {
            var builder = new CharacterMapBuilder();

            builder.AddBaseFontCharacter(new byte[] { 0x00, 0x03 }, " "); // regular space, first
            builder.AddBaseFontCharacter(new byte[] { 0x00, 0x03 }, " "); // non-breaking space, later

            Assert.Equal(" ", builder.BaseFontCharacterMap[3]);
        }

        // Documents the known caveat of the stateless resolution: a duplicate that is NOT zero-padded
        // is treated as the shortest representation of its value and therefore always overwrites, so
        // the LAST mapping wins (this matches PdfBox). Equal byte-length on its own does not imply
        // first-wins - the zero-padding is what drives the precedence.
        // This test could be removed if the behaviour changes.
        [Fact]
        public void LastMappingWinsForNonZeroPaddedDuplicateCode()
        {
            var builder = new CharacterMapBuilder();

            builder.AddBaseFontCharacter(new byte[] { 0x41, 0x42 }, "A"); // first
            builder.AddBaseFontCharacter(new byte[] { 0x41, 0x42 }, "B"); // later

            Assert.Equal("B", builder.BaseFontCharacterMap[0x4142]);
        }
    }
}
