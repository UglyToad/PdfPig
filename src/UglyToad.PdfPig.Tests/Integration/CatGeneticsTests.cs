namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;
    using Xunit;

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
            using (var document = PdfDocument.Open(GetFilename(), new ParsingOptions { UseLenientParsing = false }))
            {
                var page = document.GetPage(1);

                var annotations = page.GetAnnotations().ToList();

                Assert.NotEmpty(annotations);
            }
        }
    }
}
