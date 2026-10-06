namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Cyclic or very deep trees in untrusted input must end in a result or a catchable exception. Before the fixes
    /// each of these documents terminated the whole process with a stack overflow, or ran until it was out of memory.
    /// </summary>
    public class CyclicAndDeepTreeTests
    {
        private const string Pages = "<< /Type /Pages /Kids [3 0 R] /Count 1 >>";
        private const string Page = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>";

        [Fact]
        public void DestsNameTreeWithSelfReferencingKidsOpens()
        {
            var pdf = Build(
                "<< /Type /Catalog /Pages 2 0 R /Names << /Dests 4 0 R >> >>", Pages, Page,
                "<< /Kids [4 0 R] >>");

            using var document = PdfDocument.Open(pdf);

            Assert.Equal(1, document.NumberOfPages);
        }

        [Fact]
        public void EmbeddedFilesNameTreeWithCycleBesideValidLeafReturnsTheFileOnce()
        {
            var pdf = Build(
                "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles 4 0 R >> >>", Pages, Page,
                "<< /Kids [5 0 R 4 0 R] >>",
                "<< /Names [(a.txt) 6 0 R] >>",
                "<< /Type /Filespec /F (a.txt) /EF << /F 7 0 R >> >>",
                "<< /Type /EmbeddedFile /Length 5 >>\nstream\nhello\nendstream");

            using var document = PdfDocument.Open(pdf);

            Assert.True(document.Advanced.TryGetEmbeddedFiles(out var files));
            var file = Assert.Single(files);
            Assert.Equal("a.txt", file.Name);
            Assert.Equal("hello", Encoding.ASCII.GetString(file.Bytes.ToArray()));
        }

        [Fact]
        public void NameTreeWithOddNamesArrayIgnoresTheDanglingKey()
        {
            var pdf = Build(
                "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [(a.txt) 4 0 R (dangling)] >> >> >>", Pages, Page,
                "<< /Type /Filespec /F (a.txt) /EF << /F 5 0 R >> >>",
                "<< /Type /EmbeddedFile /Length 5 >>\nstream\nhello\nendstream");

            using var document = PdfDocument.Open(pdf);

            Assert.True(document.Advanced.TryGetEmbeddedFiles(out var files));
            Assert.Equal("a.txt", Assert.Single(files).Name);
        }

        private static byte[] Build(params string[] objects)
        {
            var builder = new StringBuilder("%PDF-1.7\n");
            var offsets = new List<int>();
            for (var i = 0; i < objects.Length; i++)
            {
                offsets.Add(builder.Length);
                builder.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xref = builder.Length;
            builder.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                builder.Append($"{offset:D10} 00000 n \n");
            }

            builder.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }
    }
}
