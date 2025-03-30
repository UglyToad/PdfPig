namespace UglyToad.PdfPig.Tests.Integration
{
    using DocumentLayoutAnalysis.PageSegmenter;
    using DocumentLayoutAnalysis.WordExtractor;
    using UglyToad.PdfPig.Core;

    public class XObjectFormTests
    {
        [Fact]
        public void CanReadDocumentWithoutStackOverflowIssue671()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("issue_671")))
            {
                var page = document.GetPage(1);
            }
        }

        [Fact]
        public void CanReadDocumentThrowsIssue671()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("issue_671"), ParsingOptions.LenientParsingOff))
            {
                var exception = Assert.Throws<PdfDocumentFormatException>(() => document.GetPage(1));
                Assert.Contains("is referencing itself which can cause unexpected behaviour", exception.Message);
            }
        }

        [Fact]
        public void CanReadDocumentMOZILLA_3136_0()
        {
            // This document does not actually contain circular references
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("MOZILLA-3136-0"), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);
            }
        }

        [Fact]
        public void XObjectFormXClipping()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("ICML03-081"), new ParsingOptions()
                   {
                       ClipPaths = true
                   }))
            {
                var page = document.GetPage(4);
                Assert.True(page.Paths.Count > 3);

                var words = page.GetWords(NearestNeighbourWordExtractor.Instance);
                var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words).SelectMany(b => b.TextLines).ToArray();

                Assert.Equal(2, blocks.Count(l => l.Text.Equals("Training Examples per Class")));
                Assert.Equal(2, blocks.Count(l => l.Text.Equals("Classification Weight")));
            }
        }
    }
}
