namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;

    public class SinglePageType1FontTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Single Page Type 1 Font.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(1, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.Letter, page.Size);
            }
        }

        [Fact]
        public void HasCorrectPageText()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal("PDF test that contains the word yadda", page.Text.Trim());
            }
        }
    }
}
