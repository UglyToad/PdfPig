namespace UglyToad.PdfPig.Tests.Fonts.CompactFontFormat
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfFonts.CompactFontFormat;
    using PdfFonts.CompactFontFormat.Dictionaries;
    using Xunit;

    public class CompactFontFormatParserTests
    {
        private readonly CompactFontFormatParser parser = new CompactFontFormatParser(
            new CompactFontFormatIndividualFontParser(new CompactFontFormatTopLevelDictionaryReader(), new CompactFontFormatPrivateDictionaryReader()));

        [Fact]
        public void CanReadMinionPro()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = parser.Parse(new CompactFontFormatData(fileBytes));

            Assert.Equal(1, font.Header.MajorVersion);
            Assert.Equal(1, font.Fonts.Count);
            Assert.True(font.Fonts.ContainsKey("MinionPro-It"));
        }

        [Fact]
        public void CanInterpretPercentSymbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = parser.Parse(new CompactFontFormatData(fileBytes));

            // Calls a global subroutine
            var box = font.GetCharacterBoundingBox("percent");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretNumberSignSymbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = parser.Parse(new CompactFontFormatData(fileBytes));

            // Calls a local subroutine
            var box = font.GetCharacterBoundingBox("numbersign");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretPerThousandSymbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = parser.Parse(new CompactFontFormatData(fileBytes));

            // Calls a local subroutine which adds to the hints
            var box = font.GetCharacterBoundingBox("perthousand");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretATildeSmallSymbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = parser.Parse(new CompactFontFormatData(fileBytes));

            // Calls a global subroutine which adds to the hints
            var box = font.GetCharacterBoundingBox("Atildesmall");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretUniF687Symbol()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = parser.Parse(new CompactFontFormatData(fileBytes));

            // Calls hugely nested subroutines
            var box = font.GetCharacterBoundingBox("uniF687");

            Assert.NotNull(box);
        }

        [Fact]
        public void CanInterpretAllGlyphs()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var fontSet = parser.Parse(new CompactFontFormatData(fileBytes));

            var font = fontSet.Fonts["MinionPro-It"];

            var charStrings = font.CharStrings.Match(x => throw new InvalidOperationException("The charstrings in MinionPro are Type 2."),
                x => x);

            foreach (var charString in charStrings.CharStrings)
            {
                var path = charStrings.Generate(charString.Key, 1000, 0);

                Assert.NotNull(path);
            }
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
