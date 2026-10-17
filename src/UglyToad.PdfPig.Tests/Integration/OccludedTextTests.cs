namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Core;

    public class OccludedTextTests
    {
        private static readonly ParsingOptions RemoveOccluded = new ParsingOptions
        {
            UseLenientParsing = false,
            RemoveOccludedText = true
        };

        [Fact]
        public void RemovesTextCoveredByLaterOpaqueRectangle()
        {
            var pdf = BuildPdf("BT /F1 12 Tf 10 50 Td (Hidden) Tj ET 1 1 1 rg 5 40 100 30 re f BT /F1 12 Tf 10 50 Td (Shown) Tj ET");

            Assert.Equal("Shown", GetText(pdf, RemoveOccluded));
        }

        [Fact]
        public void RemovesCoveredTextWithFontBoundingBoxGlyphs()
        {
            // Without the font program the glyph box is the font's /FontBBox, wider than the letter.
            var pdf = BuildPdf("BT /F2 10 Tf 10 50 Td (M) Tj ET 1 1 1 rg 10 40 100 30 re f");

            Assert.Equal(string.Empty, GetText(pdf, RemoveOccluded));
        }

        [Fact]
        public void RemovesCoveredTextFromMarkedContent()
        {
            var pdf = BuildPdf("/P << /MCID 0 >> BDC BT /F1 12 Tf 10 50 Td (Hidden) Tj ET EMC 1 1 1 rg 5 40 100 30 re f");

            using (var document = PdfDocument.Open(pdf, RemoveOccluded))
            {
                var markedContent = Assert.Single(document.GetPage(1).GetMarkedContents());
                Assert.Empty(markedContent.Letters);
            }
        }

        [Fact]
        public void KeepsCoveredTextWhenOptionIsOff()
        {
            var pdf = BuildPdf("BT /F1 12 Tf 10 50 Td (Hidden) Tj ET 1 1 1 rg 5 40 100 30 re f");

            Assert.Equal("Hidden", GetText(pdf, ParsingOptions.LenientParsingOff));
        }

        [Fact]
        public void RemovesOnlyFullyCoveredLetters()
        {
            // Covers "P" fully and only half of "a".
            var pdf = BuildPdf("BT /F1 12 Tf 10 50 Td (Partly) Tj ET 1 1 1 rg 5 40 14 30 re f");

            Assert.Equal("artly", GetText(pdf, RemoveOccluded));
        }

        [Fact]
        public void KeepsTextUnderTransparentFill()
        {
            var pdf = BuildPdf("BT /F1 12 Tf 10 50 Td (Seen) Tj ET /GS1 gs 1 1 1 rg 5 40 100 30 re f");

            Assert.Equal("Seen", GetText(pdf, RemoveOccluded));
        }

        [Fact]
        public void KeepsTextWhenFillIsClippedAway()
        {
            // The clip leaves the fill only below the text.
            var pdf = BuildPdf("BT /F1 12 Tf 10 50 Td (Seen) Tj ET q 0 0 200 20 re W n 1 1 1 rg 5 0 100 100 re f Q");

            Assert.Equal("Seen", GetText(pdf, RemoveOccluded));
        }

        private static string GetText(byte[] pdf, ParsingOptions options)
        {
            using (var document = PdfDocument.Open(pdf, options))
            {
                return document.GetPage(1).Text;
            }
        }

        private static byte[] BuildPdf(string content)
        {
            var objects = new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 100] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> /ExtGState << /GS1 << /ca 0.5 >> >> >> >>",
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                "<< /Type /Font /Subtype /TrueType /BaseFont /NotARealFont /FirstChar 77 /LastChar 77 /Widths [833] /FontDescriptor 7 0 R >>",
                "<< /Type /FontDescriptor /FontName /NotARealFont /Flags 32 /FontBBox [-628 -376 2000 1010] /ItalicAngle 0 /Ascent 905 /Descent -212 /CapHeight 716 /StemV 80 >>"
            };

            var pdf = new System.Text.StringBuilder("%PDF-1.7\n");
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
            return OtherEncodings.StringAsLatin1Bytes(pdf.ToString());
        }
    }
}
