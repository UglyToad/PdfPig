namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;

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

        [Fact]
        public void GetsCorrectPageTextIgnoringHiddenCharacters()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var text = string.Join(string.Empty, page.Letters.Select(x => x.Value));

                Assert.Equal("36pt font14 pt font6pt font", text);
            }
        }
    }
}
