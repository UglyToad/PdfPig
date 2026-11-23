namespace UglyToad.PdfPig.Tests.Integration
{
    public class Type3FontTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("type3-font-zero-height.pdf");
        }

        [Fact]
        public void HasLetterWidthsAndHeights()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains(page.Letters, x => x.BoundingBox.Width != 0);
                Assert.Contains(page.Letters, x => x.BoundingBox.Height != 0);
            }
        }

        [Fact]
        public void HandlesDollarPrefixedNumberInFontBBox()
        {
            var objects = new[]
            {
                "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
                "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
                "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>\nendobj\n",
                "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
                "5 0 obj\n<< /Type /Font /Subtype /Type3 /FontBBox [ -3 -9$38 28 ] /FontMatrix [ 0.001 0 0 0.001 0 0 ] /FirstChar 32 /LastChar 32 /Widths [ 600 ] /Encoding << /Type /Encoding /Differences [ 32 /space ] >> /CharProcs << /space 7 0 R >> >>\nendobj\n",
                "6 0 obj\n<< /Length 33 >>\nstream\nBT /F1 12 Tf 10 100 Td (Hi) Tj ET\nendstream\nendobj\n",
                "7 0 obj\n<< /Length 6 >>\nstream\n0 0 d0\nendstream\nendobj\n",
            };

            var sb = new System.Text.StringBuilder("%PDF-1.4\n");
            var offsets = new int[objects.Length];
            for (var i = 0; i < objects.Length; i++)
            {
                offsets[i] = sb.Length;
                sb.Append(objects[i]);
            }

            var xrefOffset = sb.Length;
            sb.Append("xref\n0 8\n0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:D10} 00000 n \n", offset);
            }

            sb.Append("trailer\n<< /Size 8 /Root 1 0 R >>\nstartxref\n");
            sb.Append(xrefOffset);
            sb.Append("\n%%EOF");

            var bytes = System.Text.Encoding.ASCII.GetBytes(sb.ToString());
            using (var document = PdfDocument.Open(bytes))
            {
                var page = document.GetPage(1);
                Assert.Equal("Hi", page.Text);
            }
        }
    }
}