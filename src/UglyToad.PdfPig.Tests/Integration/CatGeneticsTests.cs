namespace UglyToad.PdfPig.Tests.Integration
{
    using Annotations;
    using PdfPig.Core;

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

                var annotations = page.GetAnnotations().ToArray();

                Assert.NotEmpty(annotations);

                var highlights = annotations.Where(x => x.Type == AnnotationType.Highlight);

                foreach (var highlight in highlights)
                {
                    Assert.NotEmpty(highlight.QuadPoints);
                }
            }
        }

        [Fact]
        public void CanSupportPageInformationNotFoundInLenientMode()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("pages-indirect-to-null.pdf");
            // Lenient Parsing On -> can process
            using (var document = PdfDocument.Open(path))
            {
                // unable to parse
                Assert.Equal(1, document.NumberOfPages);
                Assert.NotNull(document.GetPage(1));
            }

            // Lenient Parsing Off -> throws
            var ex = Assert.Throws<PdfDocumentFormatException>(() => PdfDocument.Open(path, ParsingOptions.LenientParsingOff));
            Assert.Equal("Pages entry is null", ex.Message);
        }
        
        [Fact]
        public void CanSupportPageKidsObjectNotBeingAPage()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("pages-kids-not-page.pdf");

            using (var document = PdfDocument.Open(path))
            {
                // unable to parse
                Assert.Equal(1, document.NumberOfPages);
                Assert.NotNull(document.GetPage(1));
            }

            // Lenient Parsing Off -> throws
            var ex = Assert.Throws<PdfDocumentFormatException>(() => PdfDocument.Open(path, ParsingOptions.LenientParsingOff));
            Assert.Equal("Could not find dictionary associated with reference in pages kids array: 3 0.", ex.Message);
        }
    }
}
