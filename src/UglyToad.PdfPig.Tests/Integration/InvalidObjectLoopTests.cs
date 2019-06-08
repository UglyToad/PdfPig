namespace UglyToad.PdfPig.Tests.Integration
{
    using Xunit;

    public class InvalidObjectLoopTests
    {
        [Fact]
        public void CanOpenDocumentAndGetPage()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("invalid-xref-loop.pdf");

            using (var document = PdfDocument.Open(path))
            {
                for (var i = 1; i <= document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i);

                    Assert.NotNull(page.Content);
                }
            }
        }
    }
}
