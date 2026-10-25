namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Globalization;
    using System.Text;
    using UglyToad.PdfPig.AcroForms;
    using UglyToad.PdfPig.AcroForms.Fields;

    /// <summary>
    /// Form field trees and marked-content sequences that nest deeply or refer back to themselves.
    /// </summary>
    public class NestedStructureTests
    {
        [Fact]
        public void FieldListedAsItsOwnKid()
        {
            var pdf = BuildPdf(
                "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 0 R] >> >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
                "<< /FT /Tx /T (a) /Parent 4 0 R /Kids [4 0 R] >>");

            using var document = PdfDocument.Open(pdf);

            Assert.True(document.TryGetForm(out var form));
            Assert.Single(form.Fields);
        }

        [Fact]
        public void TwoFieldsListingEachOtherAsKids()
        {
            var pdf = BuildPdf(
                "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 0 R] >> >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
                "<< /T (a) /Parent 5 0 R /Kids [5 0 R] >>",
                "<< /FT /Tx /T (b) /Parent 4 0 R /Kids [4 0 R] >>");

            using var document = PdfDocument.Open(pdf);

            Assert.True(document.TryGetForm(out var form));
            Assert.Single(form.GetFields());
        }

        [Fact]
        public void FieldKidListedTwiceAtEveryLevel()
        {
            // Field i lists field i + 1 twice; read as written, the tree has 2^24 copies of the last field.
            const int levels = 24;
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 0 R] >> >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
            };

            for (var i = 0; i < levels; i++)
            {
                var number = 4 + i;
                var parent = i == 0 ? string.Empty : $"/Parent {number - 1} 0 R ";
                objects.Add($"<< /T (f{i}) {parent}/Kids [{number + 1} 0 R {number + 1} 0 R] >>");
            }

            objects.Add($"<< /FT /Tx /T (last) /Parent {3 + levels} 0 R >>");

            using var document = PdfDocument.Open(BuildPdf(objects.ToArray()));

            Assert.True(document.TryGetForm(out var form));
            Assert.Single(form.GetFields());
        }

        [Fact]
        public void DeeplyNestedFields()
        {
            const int levels = 10_000;
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 0 R] >> >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
            };

            for (var i = 0; i < levels; i++)
            {
                var number = 4 + i;
                var parent = i == 0 ? string.Empty : $"/Parent {number - 1} 0 R ";
                objects.Add($"<< /T (f{i}) {parent}/Kids [{number + 1} 0 R] >>");
            }

            objects.Add($"<< /FT /Tx /T (last) /Parent {3 + levels} 0 R >>");

            using var document = PdfDocument.Open(BuildPdf(objects.ToArray()));

            Assert.True(document.TryGetForm(out var form));
            Assert.Single(form.Fields);

            // The text field is nested past the depth that is read.
            Assert.Empty(form.GetFields());
        }

        [Fact]
        public void KidAlsoListedInFieldsBeforeItsParentStaysUnderItsParent()
        {
            var pdf = BuildPdf(
                "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [5 0 R 4 0 R] >> >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
                "<< /T (parent) /Kids [5 0 R] >>",
                "<< /FT /Tx /T (kid) /Parent 4 0 R >>");

            using var document = PdfDocument.Open(pdf);

            Assert.True(document.TryGetForm(out var form));

            var root = Assert.IsType<AcroNonTerminalField>(Assert.Single(form.Fields));
            Assert.Equal("parent", root.Information.PartialName);

            var kid = Assert.Single(root.Children);
            Assert.Equal("kid", kid.Information.PartialName);
            Assert.Equal(AcroFieldType.Text, kid.FieldType);
        }

        [Fact]
        public void DeeplyNestedMarkedContent()
        {
            // An optional content group at the bottom of 400,000 nested marked-content sequences.
            const int depth = 400_000;
            var content = new StringBuilder();
            for (var i = 0; i < depth; i++)
            {
                content.Append("/P BMC\n");
            }

            content.Append("/OC << /Type /OCG /Name (Layer) >> BDC\nEMC\n");

            for (var i = 0; i < depth; i++)
            {
                content.Append("EMC\n");
            }

            var pdf = BuildPdf(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] /Contents 4 0 R >>",
                Stream(content.ToString()));

            using var document = PdfDocument.Open(pdf);

            var optionalContents = document.GetPage(1).GetOptionalContents();

            Assert.Single(optionalContents["Layer"]);
        }

        private static string Stream(string content)
        {
            return $"<< /Length {Encoding.ASCII.GetByteCount(content).ToString(CultureInfo.InvariantCulture)} >>\nstream\n{content}\nendstream";
        }

        /// <summary>
        /// Writes objects[i] as object i + 1, with object 1 as the catalog.
        /// </summary>
        private static byte[] BuildPdf(params string[] objects)
        {
            using var ms = new MemoryStream();
            var offsets = new long[objects.Length + 1];

            void Write(string s)
            {
                var bytes = Encoding.ASCII.GetBytes(s);
                ms.Write(bytes, 0, bytes.Length);
            }

            Write("%PDF-1.7\n");

            for (var i = 0; i < objects.Length; i++)
            {
                offsets[i + 1] = ms.Position;
                Write($"{(i + 1).ToString(CultureInfo.InvariantCulture)} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xref = ms.Position;
            Write($"xref\n0 {(objects.Length + 1).ToString(CultureInfo.InvariantCulture)}\n");
            Write("0000000000 65535 f \n");
            for (var i = 1; i <= objects.Length; i++)
            {
                Write($"{offsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            Write($"trailer\n<< /Size {(objects.Length + 1).ToString(CultureInfo.InvariantCulture)} /Root 1 0 R >>\nstartxref\n");
            Write(xref.ToString(CultureInfo.InvariantCulture));
            Write("\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
