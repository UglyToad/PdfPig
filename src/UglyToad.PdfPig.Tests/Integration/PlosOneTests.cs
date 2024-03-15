namespace UglyToad.PdfPig.Tests.Integration
{
    public class PlosOneTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("journal.pone.0196757.pdf");
        }

        [Fact]
        public void CanReadPageOneContent()
        {
            using (var document = PdfDocument.Open(GetFilename().Replace("ICML03-081.pdf", "journal.pone.0196757.pdf")))
            {
                var page = document.GetPage(1);
                var text = page.Text;
                Assert.True(text.Length > 50);
            }
        }
    }
}
