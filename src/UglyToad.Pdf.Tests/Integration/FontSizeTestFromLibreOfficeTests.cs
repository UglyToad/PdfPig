namespace UglyToad.Pdf.Tests.Integration
{
    using System;
    using System.IO;
    using Content;
    using Xunit;

    public class FontSizeTestFromLibreOfficeTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Font Size Test - from libre office.pdf");
        }

        [Fact]
        public void GetsCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var pageCount = document.NumberOfPages;

                Assert.Equal(1, pageCount);
            }
        }

        [Fact]
        public void GetsCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.A4, page.Size);
            }
        }
    }
}
