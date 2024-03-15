namespace UglyToad.PdfPig.Tests.Integration
{
    public class GamebookTests
    {
        [Fact]
        public void Issue682()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Gamebook.pdf")))
            {
                foreach (var page in document.GetPages())
                {
                    Assert.NotNull(page);
                }
            }
        }
    }
}
