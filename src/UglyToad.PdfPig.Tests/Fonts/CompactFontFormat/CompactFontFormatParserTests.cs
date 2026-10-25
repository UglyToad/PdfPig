namespace UglyToad.PdfPig.Tests.Fonts.CompactFontFormat
{
    using System.Buffers.Binary;
    using PdfPig.Fonts;
    using PdfPig.Fonts.CompactFontFormat;

    public class CompactFontFormatParserTests
    {
        [Fact]
        public void CanReadMinionPro()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(fileBytes));

            Assert.Equal(1, font.Header.MajorVersion);
            Assert.Single(font.Fonts);
            Assert.True(font.Fonts.ContainsKey("MinionPro-It"));
        }

        [Fact]
        public void CanReadCffTableOfOpenTypeFont()
        {
            // An OpenType font with PostScript outlines ("OTTO"), as a PDF embeds it in FontFile3 /Subtype /OpenType.
            var openType = TestFontPrograms.OpenTypeCffFont(65);

            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(openType));
            var expected = CompactFontFormatParser.Parse(new CompactFontFormatData(GetFileBytes("MinionPro.bin")));

            Assert.Single(font.Fonts);
            Assert.True(font.Fonts.ContainsKey("MinionPro-It"));
            Assert.Equal(expected.GetCharacterBoundingBox("A"), font.GetCharacterBoundingBox("A"));
        }

        [Fact]
        public void CanReadCffTableOfFontTaggedAsTrueType()
        {
            // A CFF table under the TrueType sfnt version (0x00010000) instead of "OTTO", as some fonts are mislabelled.
            var openType = TestFontPrograms.OpenTypeCffFont(65);
            new byte[] { 0x00, 0x01, 0x00, 0x00 }.CopyTo(openType, 0);

            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(openType));
            var expected = CompactFontFormatParser.Parse(new CompactFontFormatData(GetFileBytes("MinionPro.bin")));

            Assert.True(font.Fonts.ContainsKey("MinionPro-It"));
            Assert.Equal(expected.GetCharacterBoundingBox("A"), font.GetCharacterBoundingBox("A"));
        }

        [Fact]
        public void TrueTypeFontWithGlyfOutlinesIsNotSupported()
        {
            var ex = Assert.Throws<NotSupportedException>(() => CompactFontFormatParser.Parse(new CompactFontFormatData(TestFontPrograms.Roboto)));

            Assert.Contains("no CFF table", ex.Message);
        }

        [Fact]
        public void OpenTypeFontWithCff2OutlinesIsNotSupported()
        {
            var cff2 = TestFontPrograms.ReplaceTable(TestFontPrograms.OpenTypeCffFont(65), "CFF ", GetFileBytes("MinionPro.bin"), "CFF2");

            var ex = Assert.Throws<NotSupportedException>(() => CompactFontFormatParser.Parse(new CompactFontFormatData(cff2)));

            Assert.Contains("CFF2", ex.Message);
        }

        [Fact]
        public void OpenTypeCffTableOutsideTheFontThrows()
        {
            var openType = TestFontPrograms.OpenTypeCffFont(65);

            // Point the 'CFF ' table record at an offset past 2^31, which a signed read would turn negative.
            BinaryPrimitives.WriteUInt32BigEndian(TestFontPrograms.TableRecord(openType, "CFF ").Slice(8), 0xFFFFFF00);

            Assert.Throws<InvalidFontFormatException>(() => CompactFontFormatParser.Parse(new CompactFontFormatData(openType)));
        }

        [Fact]
        public void CanInterpretPercentSymbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(fileBytes));

            // Calls a global subroutine
            var box = font.GetCharacterBoundingBox("percent");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretNumberSignSymbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(fileBytes));

            // Calls a local subroutine
            var box = font.GetCharacterBoundingBox("numbersign");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretPerThousandSymbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(fileBytes));

            // Calls a local subroutine which adds to the hints
            var box = font.GetCharacterBoundingBox("perthousand");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretATildeSmallSymbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(fileBytes));

            // Calls a global subroutine which adds to the hints
            var box = font.GetCharacterBoundingBox("Atildesmall");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretUniF687Symbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(fileBytes));

            // Calls hugely nested subroutines
            var box = font.GetCharacterBoundingBox("uniF687");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretAllGlyphs()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var fontSet = CompactFontFormatParser.Parse(new CompactFontFormatData(fileBytes));

            var font = fontSet.Fonts["MinionPro-It"];

            var charStrings = font.CharStrings.Match(x => throw new InvalidOperationException("The charstrings in MinionPro are Type 2."),
                x => x);

            foreach (var charString in charStrings.CharStrings)
            {
                var path = charStrings.Generate(charString.Key, 1000, 0);

                Assert.NotNull(path);
            }
        }

        [Fact]
        public void CanReadCidFontWithEmptyFontDictionaryInFdArray()
        {
            var font = ParseCidFont(SyntheticCidKeyedFont.FirstFontDictionary.Empty);

            // One entry per FDArray entry, in the original order. FDSelect yields the
            // index into these lists, so an emptied entry has to keep its place rather
            // than be skipped over.
            Assert.Equal(2, font.FontDictionaries.Count);
            Assert.Equal(2, font.PrivateDictionaries.Count);

            Assert.Null(font.FontDictionaries[0].PrivateDictionaryLocation);
            Assert.NotNull(font.FontDictionaries[1].PrivateDictionaryLocation);

            // What makes a font CID keyed is the presence of the ROS operator, not the
            // character collection it names, so the fixture uses identifiers of its own.
            Assert.Equal("Test", font.TopDictionary.CidFontOperators.Ros.Registry);
            Assert.Equal("Sample", font.TopDictionary.CidFontOperators.Ros.Ordering);
        }

        [Fact]
        public void CanReadCidFontWithZeroSizePrivateDictionary()
        {
            var font = ParseCidFont(SyntheticCidKeyedFont.FirstFontDictionary.WithZeroSizePrivateDictionary);

            Assert.Equal(2, font.PrivateDictionaries.Count);
        }

        [Fact]
        public void CanStillReadCidFontWithPrivateDictionaryInEveryFontDictionary()
        {
            var font = ParseCidFont(SyntheticCidKeyedFont.FirstFontDictionary.WithPrivateDictionary);

            Assert.Equal(2, font.PrivateDictionaries.Count);
            Assert.NotNull(font.FontDictionaries[0].PrivateDictionaryLocation);
            Assert.NotNull(font.FontDictionaries[1].PrivateDictionaryLocation);
        }

        private static CompactFontFormatCidFont ParseCidFont(SyntheticCidKeyedFont.FirstFontDictionary first)
        {
            var fontSet = CompactFontFormatParser.Parse(
                new CompactFontFormatData(SyntheticCidKeyedFont.Build(first)));

            return Assert.IsType<CompactFontFormatCidFont>(fontSet.FirstFont);
        }

        private static byte[] GetFileBytes(string name)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fonts", "CompactFontFormat"));
            var files = Directory.GetFiles(documentFolder);

            var file = files.FirstOrDefault(x => x.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);

            if (file == null)
            {
                throw new InvalidOperationException($"Could not find test file {name} in folder {documentFolder}.");
            }

            return File.ReadAllBytes(file);
        }
    }
}
