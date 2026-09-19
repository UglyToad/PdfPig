using System.Globalization;
using System.IO;
using System.Text;

namespace UglyToad.PdfPig.Tests.Graphics
{
    /// <summary>
    /// A content stream is tokenized without deciding what its strings mean, because the operand of
    /// a text showing operator is a sequence of character codes rather than text. Entries the
    /// specification types as text strings, such as <c>/ActualText</c> in a marked-content property
    /// dictionary, are read as text when something asks for it, so a producer writing the
    /// spec-correct UTF-16BE form gets the text back rather than its bytes one by one.
    /// </summary>
    public class InlineMarkedContentTextStringTests
    {
        [Fact]
        public void InlinePropertiesReadTheirTextStrings()
        {
            var contents = new MemoryStream();

            void WriteAscii(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                contents.Write(b, 0, b.Length);
            }

            WriteAscii("/Span <</ActualText (");
            // A byte order mark followed by "Hi" in UTF-16BE. None of these bytes need escaping.
            contents.Write([0xFE, 0xFF, 0x00, 0x48, 0x00, 0x69], 0, 6);
            WriteAscii(")>> BDC\nEMC\n");

            using var document = PdfDocument.Open(BuildSinglePagePdf(contents.ToArray()));

            var markedContent = Assert.Single(document.GetPage(1).GetMarkedContents());

            Assert.Equal("Hi", markedContent.ActualText);
        }

        private static byte[] BuildSinglePagePdf(byte[] contentStream)
        {
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
            Write("3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources << >> "
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
