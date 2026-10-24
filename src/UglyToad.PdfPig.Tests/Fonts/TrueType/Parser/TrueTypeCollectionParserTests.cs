namespace UglyToad.PdfPig.Tests.Fonts.TrueType.Parser
{
    using System.IO;
    using System.Linq;
    using PdfPig.Fonts;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using static UglyToad.PdfPig.Tests.Fonts.TestFontPrograms;

    public class TrueTypeCollectionParserTests
    {
        internal const string WindowsCollection = @"C:\Windows\Fonts\msgothic.ttc";

        [Fact]
        public void ParsesEveryFontInCollectionOrder()
        {
            var collection = FontCollection(
                SingleGlyphFont('A', unicode: true, glyph: 'a', postScriptName: "First-Face"),
                SingleGlyphFont('B', unicode: true, glyph: 'o', postScriptName: "Second-Face"));

            var fonts = TrueTypeFontParser.ParseCollection(new TrueTypeDataBytes(collection));

            Assert.Equal(new[] { "First-Face", "Second-Face" }, fonts.Select(f => f.TableRegister.NameTable!.GetPostscriptName()));

            // Each face reads its own tables, from offsets relative to the start of the file.
            Assert.True(fonts[0].TryGetPath('A', out _));
            Assert.False(fonts[0].TryGetPath('B', out _));
            Assert.True(fonts[1].TryGetPath('B', out _));
        }

        [Fact]
        public void SkipsFontThatCannotBeParsed()
        {
            var collection = FontCollection(new byte[64], SingleGlyphFont('A', unicode: true, postScriptName: "Readable"));

            var fonts = TrueTypeFontParser.ParseCollection(new TrueTypeDataBytes(collection));

            Assert.Equal("Readable", Assert.Single(fonts).TableRegister.NameTable!.GetPostscriptName());
        }

        [Fact]
        public void SingleFontIsNotACollection()
        {
            Assert.Throws<InvalidFontFormatException>(() => TrueTypeFontParser.ParseCollection(new TrueTypeDataBytes(Roboto)));
        }

        [Fact]
        public void CollectionClaimingMoreFontsThanItHoldsIsRejected()
        {
            byte[] header = [(byte)'t', (byte)'t', (byte)'c', (byte)'f', 0, 1, 0, 0, 0x7F, 0xFF, 0xFF, 0xFF];

            Assert.Throws<InvalidFontFormatException>(() => TrueTypeFontParser.ParseCollection(new TrueTypeDataBytes(header)));
        }

        [Fact]
        public void EmptyCollectionHasNoFonts()
        {
            Assert.Empty(TrueTypeFontParser.ParseCollection(new TrueTypeDataBytes(FontCollection())));
        }

        [SkippableFact]
        public void ParsesAnInstalledCollection()
        {
            Skip.IfNot(File.Exists(WindowsCollection), $"Skipped because {WindowsCollection} is not installed.");

            var fonts = TrueTypeFontParser.ParseCollection(new TrueTypeDataBytes(File.ReadAllBytes(WindowsCollection)));

            Assert.True(fonts.Count > 1);
            Assert.Contains(fonts, f => f.TableRegister.NameTable?.GetPostscriptName() == "MS-Gothic");
            Assert.All(fonts, f => Assert.True(f.TryGetPath('A', out _)));
        }
    }
}
