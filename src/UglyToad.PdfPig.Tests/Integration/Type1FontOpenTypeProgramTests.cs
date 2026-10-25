namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Linq;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Fonts.CompactFontFormat;
    using UglyToad.PdfPig.PdfFonts.Simple;
    using static UglyToad.PdfPig.Tests.Fonts.TestFontPrograms;
    using static UglyToad.PdfPig.Tests.Integration.SingleFontPdf;

    /// <summary>
    /// A Type 1 font whose program is embedded as FontFile3 /Subtype /OpenType: an OpenType font with PostScript
    /// outlines ("OTTO"), as in the Ghent Workgroup GWG091 OpenType font support test.
    /// </summary>
    public class Type1FontOpenTypeProgramTests
    {
        [Fact]
        public void UsesTheCffOutlinesOfTheEmbeddedOpenTypeProgram()
        {
            string program = BitConverter.ToString(OpenTypeCffFont(65)).Replace("-", string.Empty) + ">";
            var pdf = BuildPdf(
                "<< /Type /Font /Subtype /Type1 /BaseFont /MinionPro-It /FirstChar 65 /LastChar 65 /Widths [600] /Encoding /WinAnsiEncoding /FontDescriptor 6 0 R >>",
                "(A)",
                "<< /Type /FontDescriptor /FontName /MinionPro-It /Flags 34 /FontBBox [-200 -300 1200 1000] /ItalicAngle -12 /Ascent 730 /Descent -270 /CapHeight 650 /StemV 80 /FontFile3 7 0 R >>",
                $"<< /Subtype /OpenType /Filter /ASCIIHexDecode /Length {program.Length} >>\nstream\n{program}\nendstream");

            using var document = PdfDocument.Open(pdf);
            var letter = document.GetPage(1).Letters.Single();
            var font = Assert.IsType<Type1FontSimple>(letter.GetFont());

            Assert.True(font.TryGetPath(65, out var path));

            var minionPro = CompactFontFormatParser.Parse(new CompactFontFormatData(MinionPro)).FirstFont;
            var expected = PdfSubpath.GetBoundingRectangle(minionPro.GetCharacterPath("A")!);
            var actual = PdfSubpath.GetBoundingRectangle(path);

            Assert.NotNull(expected);
            Assert.Equal(expected, actual);
        }
    }
}
