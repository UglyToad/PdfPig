namespace UglyToad.PdfPig.Tests.Integration
{
    using Xunit;

    public class OldGutnishTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Old Gutnish Internet Explorer.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(3, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectContentAfterReadingPreviousPage()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("Þissi þieluar hafþi ann sun sum hit hafþi. En hafþa cuna hit huita stierna", page.Text);

                page = document.GetPage(2);

                Assert.Contains("Greipur sem annar hét; og Gunnfjón sá þriðji", page.Text);
            }
        }

        [Fact]
        public void GetsImageOnPageOne()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                page.Content.GetImages();
            }
        }

    }
}