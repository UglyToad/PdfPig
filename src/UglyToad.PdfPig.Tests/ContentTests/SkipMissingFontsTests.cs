namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Fonts;
    using Xunit;

    /// <summary>
    /// With <see cref="ParsingOptions.SkipMissingFonts"/> set, a font that can't be loaded is skipped
    /// however the page declares it, and text in the page's other fonts is still read.
    /// </summary>
    public class SkipMissingFontsTests
    {
        private const string Content = "BT /F1 12 Tf 10 100 Td (A) Tj /F2 12 Tf (B) Tj ET";

        private const string Helvetica = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

        // A Type 0 font must have a descendant font.
        private const string BrokenType0 = "<< /Type /Font /Subtype /Type0 /BaseFont /Broken /Encoding /Identity-H >>";

        [Fact]
        public void SkipsFontReferenceToNonDictionary()
        {
            var pdf = Build("/F1 6 0 R /F2 " + Helvetica, "(not a font)");

            Assert.Equal("B", ReadLetters(pdf, skipMissingFonts: true));
        }

        [Fact]
        public void SkipsDirectFontThatFailsToLoad()
        {
            var pdf = Build("/F1 " + BrokenType0 + " /F2 " + Helvetica);

            Assert.Equal("B", ReadLetters(pdf, skipMissingFonts: true));
        }

        [Fact]
        public void StillThrowsForBrokenFontsWhenNotSkipping()
        {
            Assert.Throws<PdfDocumentFormatException>(() => ReadLetters(Build("/F1 6 0 R /F2 " + Helvetica, "(not a font)"), skipMissingFonts: false));
            Assert.Throws<InvalidFontFormatException>(() => ReadLetters(Build("/F1 " + BrokenType0 + " /F2 " + Helvetica), skipMissingFonts: false));
        }

        private static string ReadLetters(byte[] pdf, bool skipMissingFonts)
        {
            using (var document = PdfDocument.Open(pdf, new ParsingOptions { SkipMissingFonts = skipMissingFonts }))
            {
                return string.Concat(document.GetPage(1).Letters.Select(l => l.Value));
            }
        }

        /// <summary>
        /// One page whose font resource dictionary holds <paramref name="fonts"/>, with optional
        /// extra objects numbered from 6.
        /// </summary>
        private static byte[] Build(string fonts, params string[] extraObjects)
        {
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << " + fonts + " >> >> /Contents 4 0 R >>",
                $"<< /Length {Content.Length} >>\nstream\n{Content}\nendstream",
                "null",
            };
            objects.AddRange(extraObjects);

            var builder = new StringBuilder("%PDF-1.7\n");
            var offsets = new List<int>();
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(builder.Length);
                builder.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xref = builder.Length;
            builder.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                builder.Append($"{offset:D10} 00000 n \n");
            }

            builder.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }
    }
}
