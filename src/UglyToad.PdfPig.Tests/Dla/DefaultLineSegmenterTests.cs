namespace UglyToad.PdfPig.Tests.Dla
{
    using System.Linq;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

    public class DefaultLineSegmenterTests
    {

        /// <summary>
        /// https://github.com/UglyToad/PdfPig/issues/844
        /// </summary>
        [Fact]
        public void CanSegmentLinesExistingFunctionalityTest()
        {

            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("history-2-3.pdf")))
            {
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters).ToArray();


                // Act
                var lines = new DefaultLineSegmenter().GetLines(words).ToList();


                Assert.Equal("1.", lines[12].Text); //Should be before but ordered out of order
                Assert.Equal("新北市金瓜石的國際終戰和平紀念園區，立有一面紀念牆，上面刻有第二次世界大戰時拘", lines[11].Text);
            }
        }


        /// <summary>
        /// https://github.com/UglyToad/PdfPig/issues/844
        /// </summary>
        [Fact]
        public void WhenToleranceIsSetLinesGetCombinededIntoActualLinesInDocument()
        {

            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("history-2-3.pdf")))
            {
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters).ToArray();


                // Act
                var lines = new DefaultLineSegmenter(10).GetLines(words).ToList();


                Assert.Equal(47, lines.Count); //Should be before but ordered out of order
                Assert.Equal("1. 新北市金瓜石的國際終戰和平紀念園區，立有一面紀念牆，上面刻有第二次世界大戰時拘", lines[5].Text); //Should be before but ordered out of order
            }
        }
    }
}
