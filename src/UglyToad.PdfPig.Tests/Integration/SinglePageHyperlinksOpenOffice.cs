namespace UglyToad.PdfPig.Tests.Integration
{
    using Xunit;

    public class SinglePageHyperlinksOpenOffice
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Single Page Hyperlinks - from open office.pdf");
        }

        [Fact]
        public void GetsCorrectText()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                Assert.Equal("https://duckduckgo.com/ a link aboveGitHub", page.Text);
            }
        }

        [Fact]
        public void GetsHyperlinks()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);
                
                var links = page.GetHyperlinks();

                Assert.Equal(2, links.Count);

                var ddg = links[0];

                Assert.Equal("https://duckduckgo.com/", ddg.Text);
                Assert.Equal("https://duckduckgo.com/", ddg.Uri);
                Assert.Equal("https://duckduckgo.com/ ".Length, ddg.Letters.Count);

                Assert.NotNull(ddg.Annotation);

                var github = links[1];

                Assert.Equal("GitHub", github.Text);
                Assert.Equal("https://github.com/", github.Uri);
                Assert.Equal(6, github.Letters.Count);

                Assert.NotNull(github.Annotation);
            }
        }
    }
}
