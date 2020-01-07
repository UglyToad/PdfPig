namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;
    using Xunit;

    public class FarmerMacTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("FarmerMac.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(5, document.NumberOfPages);
            }
        }

        [Fact]
        public void GetPagesWorks()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var pageCount = document.GetPages().Count();

                Assert.Equal(5, pageCount);
            }
        }

        [Fact]
        public void HasCorrectContentAfterReadingPreviousPage()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                // ReSharper disable once RedundantAssignment
                var page = document.GetPage(1);
                page = document.GetPage(2);

                Assert.Contains("financial results for the fiscal quarter ended June 30, 2017 and (2) a conference call to discuss those results and Farmer Mac", page.Text);
            }
        }

    }
}