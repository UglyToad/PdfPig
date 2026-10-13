using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace UglyToad.PdfPig.Tests.Graphics
{
    /// <summary>
    /// A combining mark (U+0301 here, code 193 in the test font) attaches to the letter before it.
    /// The mark can be drawn by its own show text operation, so attaching must not depend on the
    /// base letter and the mark sharing a single Tj or TJ. See issue #1402.
    /// </summary>
    public class CombiningDiacriticTextObjectTests
    {
        [Fact]
        public void MarkInSeparateTjCombinesWithPreviousLetter()
        {
            var letters = Letters("BT /F1 24 Tf 50 50 Td (e) Tj (\\301) Tj ET\n");

            Assert.Equal(new[] { "e\u0301" }, letters);
        }

        [Fact]
        public void MarkInSameTjCombinesWithPreviousLetter()
        {
            var letters = Letters("BT /F1 24 Tf 50 50 Td (e\\301) Tj ET\n");

            Assert.Equal(new[] { "e\u0301" }, letters);
        }

        [Fact]
        public void MarkInSeparateTjArrayCombinesWithPreviousLetter()
        {
            var letters = Letters("BT /F1 24 Tf 50 50 Td [(e)] TJ [(\\301)] TJ ET\n");

            Assert.Equal(new[] { "e\u0301" }, letters);
        }

        [Fact]
        public void MarkInLaterTextObjectDoesNotCombine()
        {
            var letters = Letters("BT /F1 24 Tf 50 50 Td (e) Tj ET\nBT /F1 24 Tf 50 20 Td (\\301) Tj ET\n");

            Assert.Equal(2, letters.Length);
            Assert.Equal("e", letters[0]);
        }

        private static string[] Letters(string content)
        {
            using var document = PdfDocument.Open(BuildSinglePagePdf(Encoding.ASCII.GetBytes(content)));

            return document.GetPage(1).Letters.Select(l => l.Value).ToArray();
        }

        private static byte[] BuildSinglePagePdf(byte[] contentStream)
        {
            const string Resources = "<< /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica "
                                     + "/Encoding << /Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [193 /acutecomb] >> >> >> >>";

            using var ms = new MemoryStream();
            var offsets = new long[5];

            void Write(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                ms.Write(b, 0, b.Length);
            }

            Write("%PDF-1.7\n");

            offsets[1] = ms.Position;
            Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            offsets[2] = ms.Position;
            Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            offsets[3] = ms.Position;
            Write($"3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources {Resources} "
                  + "/MediaBox [0 0 100 100] /Contents 4 0 R >>\nendobj\n");

            offsets[4] = ms.Position;
            Write($"4 0 obj\n<< /Length {contentStream.Length.ToString(CultureInfo.InvariantCulture)} >>\nstream\n");
            ms.Write(contentStream, 0, contentStream.Length);
            Write("\nendstream\nendobj\n");

            var xref = ms.Position;
            Write("xref\n0 5\n");
            Write("0000000000 65535 f \n");
            for (int i = 1; i <= 4; i++)
            {
                Write($"{offsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            Write("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n");
            Write(xref.ToString(CultureInfo.InvariantCulture));
            Write("\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
