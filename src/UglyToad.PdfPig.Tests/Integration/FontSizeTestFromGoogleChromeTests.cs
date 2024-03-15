namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;

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

        [Fact]
        public void GetsCorrectPageTextIgnoringHiddenCharacters()
        {
            using (var document  = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var text = string.Join(string.Empty, page.Letters.Select(x => x.Value));

                Assert.Equal("Hello, this is 16ptHello, this is 16px", text);
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
