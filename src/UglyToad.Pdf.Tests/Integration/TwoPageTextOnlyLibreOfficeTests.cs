namespace UglyToad.Pdf.Tests.Integration
{
    using System;
    using System.IO;
    using Content;
    using Xunit;

    public class TwoPageTextOnlyLibreOfficeTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Two Page Text Only - from libre office.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(2, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.A4, page.Size);

                page = document.GetPage(2);

                Assert.Equal(PageSize.A4, page.Size);
            }
        }

        [Fact]
        public void PagesStartWithCorrectText()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.StartsWith("Apache License", page.Text);

                page = document.GetPage(2);

                Assert.StartsWith("2. Grant of Copyright", page.Text);
            }
        }
    }
}
