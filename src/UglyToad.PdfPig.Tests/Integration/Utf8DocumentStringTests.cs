namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A UTF-8 text string has to survive the whole way to the document information, the outline and
    /// anywhere else a text string is surfaced, in both the literal and the hexadecimal form.
    /// </summary>
    public class Utf8DocumentStringTests
    {
        private const string Title = "Rapport annuel \u2014 \u00e9dition 2026";
        private const string Author = "Bj\u00f6rk \u00d3 Se\u00e1n";

        [Fact]
        public void Utf8DocumentInformationIsRead()
        {
            using var document = PdfDocument.Open(BuildWithInformation());

            Assert.Equal(Title, document.Information.Title);
            Assert.Equal(Author, document.Information.Author);
        }

        private static byte[] BuildWithInformation()
        {
            using var ms = new MemoryStream();
            var offsets = new long[5];

            void Write(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                ms.Write(b, 0, b.Length);
            }

            void WriteBytes(byte[] b) => ms.Write(b, 0, b.Length);

            Write("%PDF-2.0\n");

            offsets[1] = ms.Position;
            Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            offsets[2] = ms.Position;
            Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            offsets[3] = ms.Position;
            Write("3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources << >> /MediaBox [0 0 200 200] >>\nendobj\n");

            // The title as a hexadecimal string and the author as a literal one, both carrying the mark.
            offsets[4] = ms.Position;
            Write("4 0 obj\n<< /Title ");
            Write(AsHex(Utf8WithBom(Title)));
            Write(" /Author (");
            WriteBytes(Utf8WithBom(Author));
            Write(") >>\nendobj\n");

            var xref = ms.Position;
            Write("xref\n0 5\n0000000000 65535 f \n");
            for (var i = 1; i <= 4; i++)
            {
                Write($"{offsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            Write("trailer\n<< /Size 5 /Root 1 0 R /Info 4 0 R >>\nstartxref\n");
            Write(xref.ToString(CultureInfo.InvariantCulture));
            Write("\n%%EOF\n");

            return ms.ToArray();
        }

        private static byte[] Utf8WithBom(string text)
        {
            var body = Encoding.UTF8.GetBytes(text);

            var result = new byte[body.Length + 3];
            result[0] = 0xEF;
            result[1] = 0xBB;
            result[2] = 0xBF;
            body.CopyTo(result, 3);

            return result;
        }

        private static string AsHex(byte[] bytes)
        {
            var builder = new StringBuilder("<");
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("X2"));
            }

            return builder.Append('>').ToString();
        }
    }
}
