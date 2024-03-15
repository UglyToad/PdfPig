namespace UglyToad.PdfPig.Tests.Integration
{
    using Annotations;

    public class CatGeneticsTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("cat-genetics.pdf");
        }

        [Fact]
        public void CanReadContent()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("catus", page.Text);
            }
        }
        
        [Fact]
        public void CanGetAnnotations()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                var annotations = page.ExperimentalAccess.GetAnnotations().ToList();

                Assert.NotEmpty(annotations);

                var highlights = annotations.Where(x => x.Type == AnnotationType.Highlight);

                foreach (var highlight in highlights)
                {
                    Assert.NotEmpty(highlight.QuadPoints);
                }
            }
        }
    }
}
