namespace UglyToad.Pdf.Tests.Integration
{
    using System;
    using System.IO;
    using Xunit;

    public class SinglePageType1FontTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Single Page Type 1 Font.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(1, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                //var page = document.GetPage(1);

                //Assert.Equal(PageSize.Letter, page.Size);
            }
        }
    }
}
