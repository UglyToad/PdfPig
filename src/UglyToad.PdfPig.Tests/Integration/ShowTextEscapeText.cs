namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Writer;
    using Xunit;

    public class ShowTextEscapeText
    {
        private static string GetFilename()
        {
            // On the single page of the source PDF has 3 ShowText operations with unbalanced round brackets in the text.
            // Unbalanced meaning there is an open bracket without a close bracket or close without open.
            // 1. line 387   (\() Tj
            // 2. line 397   (\)) Tj
            // 3. line 384   ( \(I\222ll try to stay on ) Tj
            // note in text 3 the 0222 (octal) or (0x92 hex) is similar to an apostople ' so text is similar to " (I'll try to stay on" (with an open bracket).
            return IntegrationHelpers.GetDocumentPath("ShowTextOpWithUnbalancedRoundBrackets.pdf");
        }

        [Fact]
        public void PdfCopyShowTextOpUsesEscapedText()
        {
            var filePath = GetFilename();
            using (var sourceDocument = PdfDocument.Open(filePath))
            {
                PdfDocumentBuilder pdfBuilder = new PdfDocumentBuilder();
                var numberOfPages = sourceDocument.NumberOfPages;
                int pageNumber = 1; ////for (int pageNumber = 1; pageNumber <= numberOfPages; pageNumber++)
                {                    
                    var sourcePage = sourceDocument.GetPage(pageNumber);
                     
                    pdfBuilder.AddPage(sourcePage.Width, sourcePage.Height).CopyFrom(sourcePage); 
                }
                var pdfBytes = pdfBuilder.Build();

                // Reread (in memory) copied PDF and check example text ("wander") exists in word extract after ShowText operation with unbalanced bracket.
                using (var document = PdfDocument.Open(pdfBytes))
                {
                    var page = document.GetPage(1);
                    var words = page.GetWords();
                    var isExpectedTextInCopiedPdf = words.Any(w => w.Text.Contains("wander"));
                    Assert.True(isExpectedTextInCopiedPdf);
                }
            }
        } 
    }
}