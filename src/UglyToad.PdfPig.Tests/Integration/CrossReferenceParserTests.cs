namespace UglyToad.PdfPig.Tests.Integration
{
    using UglyToad.PdfPig.Core;

    public class CrossReferenceParserTests
    {
        [Fact]
        public void CanReadDocumentWithMissingWhitespaceAfterXRef()
        {
            string path = IntegrationHelpers.GetSpecificTestDocumentPath("xref-with-no-whitespace.pdf");
            using var document = PdfDocument.Open(path);
            Assert.Equal(3, document.NumberOfPages);
        }

        [Fact]
        public void CanReadDocumentWithCircularXRef()
        {
            string path = IntegrationHelpers.GetSpecificTestDocumentPath("B17-2000-transportation-fuels.pdf");

            // If parser can't deal with xrefs that have circular references then
            // opening the document will loop forever
            using var document = PdfDocument.Open(path);

            Assert.Equal(1, document.NumberOfPages);
        }

        [Fact]
        public void CanHandleInvalidFieldSize()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("GHOSTSCRIPT-695040-0.zip-89");
            var ex = Assert.Throws<PdfDocumentFormatException>(() => PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }));
            Assert.Equal("The root object in the trailer did not resolve to a readable dictionary.", ex.Message);

            // NB: There might be a way to not throw when lenient is ON, out of scope for now
        }

        [Fact]
        public void CanReadXRefStreamWithInvalidPredictorColumns()
        {
            // The xref stream locating the page fonts and colour spaces declares /Columns 4444444444444444444444444444444.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("GHOSTSCRIPT-695040-0.zip-87");
            using var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true });

            Assert.Equal(10, document.NumberOfPages);

            var page = document.GetPage(1);
            Assert.Contains("Avant d’entamer la procédure de demande d’intervention financière pour effectuer un séjour", page.Text);
        }
    }
}
