namespace UglyToad.PdfPig.Tests.Fonts.CompactFontFormat
{
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
