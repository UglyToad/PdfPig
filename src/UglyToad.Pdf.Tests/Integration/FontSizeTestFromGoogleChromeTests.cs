namespace UglyToad.Pdf.Tests.Integration
{
    using System;
    using System.IO;
    using Xunit;

    public class FontSizeTestFromGoogleChromeTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Font Size Test - from google chrome print pdf.pdf");
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
        public void GetsCorrectPageWidthAndHeight()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(595, page.Width);
                Assert.Equal(842, page.Height);
            }
        }
    }
}
