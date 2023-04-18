namespace UglyToad.PdfPig.Tests.Integration
{
    using Xunit;

    public class Type3FontTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("type3-font-zero-height.pdf");
        }

        [Fact]
        public void HasLetterWidthsAndHeights()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains(page.Letters, x => x.GlyphRectangle.Width != 0);
                Assert.Contains(page.Letters, x => x.GlyphRectangle.Height != 0);
            }
        }
    }
}