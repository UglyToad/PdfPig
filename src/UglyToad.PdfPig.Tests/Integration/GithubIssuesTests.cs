namespace UglyToad.PdfPig.Tests.Integration
{
    public class GithubIssuesTests
    {
        [Fact]
        public void Issue874()
        {
            var doc = IntegrationHelpers.GetDocumentPath("ErcotFacts.pdf");

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page1 = document.GetPage(1);
                Assert.Equal(1788, page1.Letters.Count);

                var page2 = document.GetPage(2);
                Assert.Equal(2430, page2.Letters.Count);
            }

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = false }))
            {
                var ex = Assert.Throws<ArgumentNullException>(() => document.GetPage(1));
                Assert.StartsWith("Value cannot be null.", ex.Message);
            }
        }
    }
}
