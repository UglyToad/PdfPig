namespace UglyToad.PdfPig.Tests.Integration
{
    public class BruteForceSearcherTests
    {
        [Fact]
        public void Pull1360()
        {
            // See https://github.com/UglyToad/PdfPig/pull/1360
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("pdfium_type3_basic.pdf");
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                foreach (var page in document.GetPages())
                {
                    Assert.NotNull(page);
                }
            }
        }
    }
}
