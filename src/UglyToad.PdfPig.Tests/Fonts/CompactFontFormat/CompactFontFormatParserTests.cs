namespace UglyToad.PdfPig.Tests.Fonts.CompactFontFormat
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfPig.Fonts.CompactFontFormat;
    using PdfPig.Fonts.CompactFontFormat.Dictionaries;
    using Xunit;

    public class CompactFontFormatParserTests
    {
        private readonly CompactFontFormatParser parser = new CompactFontFormatParser(new CompactFontFormatIndividualFontParser(
                new CompactFontFormatIndexReader(), new CompactFontFormatTopLevelDictionaryReader(), new CompactFontFormatPrivateDictionaryReader()), 
            new CompactFontFormatIndexReader());

        [Fact]
        public void CanReadMinionPro()
        {
            var fileBytes = GetFileBytes("MinionPro.bin");

            var font = parser.Parse(new CompactFontFormatData(fileBytes));

            Assert.Equal("MinionPro", font.ToString());
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
