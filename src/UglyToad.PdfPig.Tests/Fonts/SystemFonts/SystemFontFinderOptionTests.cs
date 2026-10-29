namespace UglyToad.PdfPig.Tests.Fonts.SystemFonts
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using PdfPig.Fonts.SystemFonts;
    using PdfPig.Fonts.TrueType;

    public class SystemFontFinderOptionTests
    {
        [Fact]
        public void NoneUsesTheStandard14Metrics()
        {
            using var document = PdfDocument.Open(NonEmbeddedArialPdf(), new ParsingOptions { SystemFontFinder = SystemFontFinder.None });

            // Helvetica's AFM: P is B 86 0 622 718, at 12 pt.
            var p = document.GetPage(1).Letters.First(l => l.Value == "P").BoundingBox;

            Assert.Equal(6.432, p.Width, 3);
            Assert.Equal(8.616, p.Height, 3);
            Assert.Equal(72 + 86 * 0.012, p.Left, 3);
        }

        [Fact]
        public void UsesTheFinderFromTheOptions()
        {
            var finder = new RecordingFinder();

            using var document = PdfDocument.Open(NonEmbeddedArialPdf(), new ParsingOptions { SystemFontFinder = finder });
            _ = document.GetPage(1).Letters;

            Assert.Contains("Arial", finder.Requested);
        }

        [Fact]
        public void NoneFindsNothing()
        {
            Assert.Null(SystemFontFinder.None.GetTrueTypeFont("Arial"));
        }

        private sealed class RecordingFinder : ISystemFontFinder
        {
            public List<string> Requested { get; } = new List<string>();

            public TrueTypeFont? GetTrueTypeFont(string name)
            {
                Requested.Add(name);
                return null;
            }
        }

        /// <summary>
        /// One page whose only font is a TrueType font named Arial with no /Widths, /FirstChar or /FontDescriptor,
        /// read with an installed font of that name where there is one.
        /// </summary>
        private static byte[] NonEmbeddedArialPdf()
        {
            const string content = "BT /F1 12 Tf 72 700 Td (Pin 1 EN Enable input) Tj ET";
            string[] objects =
            [
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
                "<< /Type /Font /Subtype /TrueType /BaseFont /Arial /Encoding /WinAnsiEncoding >>",
            ];

            var pdf = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int>();
            for (var i = 0; i < objects.Length; i++)
            {
                offsets.Add(pdf.Length);
                pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xref = pdf.Length;
            pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                pdf.Append($"{offset:D10} 00000 n \n");
            }

            pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");

            return Encoding.ASCII.GetBytes(pdf.ToString());
        }
    }
}
