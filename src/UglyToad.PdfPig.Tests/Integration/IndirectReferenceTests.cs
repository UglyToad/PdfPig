namespace UglyToad.PdfPig.Tests.Integration
{
    using DocumentLayoutAnalysis.WordExtractor;
    using System.Linq;

    public class IndirectReferenceTests
    {
        [Fact]
        public void Generation65536()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("93101_1.pdf")))
            {
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters).ToArray();
                Assert.NotEmpty(words);
                
                Assert.Equal("Railway", words[0].Text);

                for (int p = 2; p <= document.NumberOfPages; ++p)
                {
                    page = document.GetPage(p);
                    Assert.NotEmpty(page.Letters);
                }
            }
        }
    }
}
