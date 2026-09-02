namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// The document catalog may carry a Version entry which supersedes the file header where it is later.
    /// A document upgraded by an incremental update declares its version that way, because an update
    /// appends to the file and so cannot rewrite the header, see ISO 32000-2, 7.5.2.
    /// </summary>
    public class CatalogVersionTests
    {
        [Theory]
        [InlineData("1.7", "/Version /2.0", 2.0)]
        [InlineData("1.4", "/Version /1.7", 1.7)]
        [InlineData("2.0", "/Version /2.1", 2.1)]
        public void CatalogVersionSupersedesAnEarlierHeader(string headerVersion, string versionEntry, double expected)
        {
            using var document = PdfDocument.Open(BuildSinglePagePdf(headerVersion, versionEntry));

            Assert.Equal(expected, document.Version);
        }

        [Theory]
        // The header wins where it is the later of the two, the catalog entry does not downgrade a document.
        [InlineData("1.7", "/Version /1.4")]
        [InlineData("1.7", "/Version /1.7")]
        // Absent, and present but unusable, both leave the header version standing.
        [InlineData("1.7", "")]
        [InlineData("1.7", "/Version /Nonsense")]
        [InlineData("1.7", "/Version (1.4)")]
        public void HeaderVersionStands(string headerVersion, string versionEntry)
        {
            using var document = PdfDocument.Open(BuildSinglePagePdf(headerVersion, versionEntry));

            Assert.Equal(double.Parse(headerVersion, CultureInfo.InvariantCulture), document.Version);
        }

        [Fact]
        public void CatalogVersionIsReadThroughAnIndirectReference()
        {
            using var document = PdfDocument.Open(
                BuildSinglePagePdf("1.7", "/Version 4 0 R", "4 0 obj\n/2.0\nendobj\n"));

            Assert.Equal(2.0, document.Version);
        }

        [Fact]
        public void CatalogVersionWrittenAsANumberIsAccepted()
        {
            // Not valid - the entry is a name object - but written by some producers, and the intent is plain.
            using var document = PdfDocument.Open(BuildSinglePagePdf("1.7", "/Version 2.0"));

            Assert.Equal(2.0, document.Version);
        }

        private static byte[] BuildSinglePagePdf(string headerVersion, string catalogVersionEntry, string extraObject = null)
        {
            using var ms = new MemoryStream();
            var count = extraObject is null ? 3 : 4;
            var offsets = new long[count + 1];

            void Write(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                ms.Write(b, 0, b.Length);
            }

            Write($"%PDF-{headerVersion}\n");

            offsets[1] = ms.Position;
            Write($"1 0 obj\n<< /Type /Catalog /Pages 2 0 R {catalogVersionEntry} >>\nendobj\n");

            offsets[2] = ms.Position;
            Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            offsets[3] = ms.Position;
            Write("3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources << >> /MediaBox [0 0 200 200] >>\nendobj\n");

            if (extraObject is not null)
            {
                offsets[4] = ms.Position;
                Write(extraObject);
            }

            var xref = ms.Position;
            Write($"xref\n0 {count + 1}\n");
            Write("0000000000 65535 f \n");
            for (var i = 1; i <= count; i++)
            {
                Write($"{offsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            Write($"trailer\n<< /Size {count + 1} /Root 1 0 R >>\nstartxref\n");
            Write(xref.ToString(CultureInfo.InvariantCulture));
            Write("\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
