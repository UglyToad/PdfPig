namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;

    public class CMapBuilderTests
    {
        [Fact]
        public void CMapCorrectMapping()
        {
            var path = IntegrationHelpers.GetDocumentPath("GHOSTSCRIPT-699178-0.pdf");
            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);
                Assert.DoesNotContain("X", page.Letters.Select(l => l.Value));
            }
        }
    }
}
