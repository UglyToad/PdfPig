namespace UglyToad.PdfPig.Tests.Dla
{
    using System.Linq;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
    using Xunit;

    public class NearestNeighbourWordExtractorTests
    {
        [Fact]
        public void Words2559Doc()
        {
            // Microsoft Word count of words = 2559

            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("2559 words.pdf")))
            {
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters).ToArray();

                var noSpacesWords = words.Where(x => !string.IsNullOrEmpty(x.Text.Trim())).ToArray();

                Assert.Equal(2559, noSpacesWords.Length);
            }
        }
    }
}
