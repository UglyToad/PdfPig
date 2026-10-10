namespace UglyToad.PdfPig.Tests.Integration
{
    public class PdfTokenScannerTests
    {
        [Fact]
        public void GHOSTSCRIPT_701622_1()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("GHOSTSCRIPT-701622-1");
            using (var document = PdfDocument.Open(path, new ParsingOptions()))
            {
                var page = document.GetPage(1);
                Assert.NotNull(page);
                Assert.Equal("Praxi", page.Text);
            }
        }
    }
}
