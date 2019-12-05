namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;
    using Xunit;

    public class PigReproductionPowerpointTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Pig Reproduction Powerpoint.pdf");
        }

        [Fact]
        public void CanReadContent()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                Assert.Contains("Pigs per sow per year: 18 to 27", page.Text);
            }
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(35, document.NumberOfPages);
            }
        }

        [Fact]
        public void CanReadAllPages()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    document.GetPage(i + 1);
                }
            }
        }

        [Fact]
        public void CanGetBookmarks()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var foundBookmarks = document.TryGetBookmarks(out var bookmarks);
                Assert.True(foundBookmarks);
                Assert.Equal(35, bookmarks.Roots.Count);
                Assert.Equal(35, bookmarks.GetNodes().Count());
            }
        }
    }
}