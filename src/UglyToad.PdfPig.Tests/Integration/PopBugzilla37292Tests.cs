namespace UglyToad.PdfPig.Tests.Integration
{
    public class PopBugzilla37292Tests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("pop-bugzilla37292.pdf");
        }

        [Fact]
        public void CanReadPages()
        {
            using (var document = PdfDocument.Open(GetFilename(), new ParsingOptions { UseLenientParsing = false }))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);
                    Assert.NotNull(page.Letters);
                }
            }
        }
    }
}
