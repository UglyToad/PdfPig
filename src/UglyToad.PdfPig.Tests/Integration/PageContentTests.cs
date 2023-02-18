namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Core;
    using Xunit;

    public class PageContentTests
    {
        [Fact]
        public void DetectPageContents()
        {
            var file = IntegrationHelpers.GetDocumentPath("Various Content Types");

            using (var document = PdfDocument.Open(file, ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);
                var letters = page.Letters;
                Assert.Contains(letters, l => l.RenderingMode == TextRenderingMode.Stroke); // "REGULAR TEXT"
                Assert.Contains(letters, l => l.RenderingMode == TextRenderingMode.Neither); // "INVISIBLE TEXT"
                Assert.NotEmpty(page.Content.GetImages());
                Assert.NotEmpty(page.Content.Paths);
            }
        }
    }
}
