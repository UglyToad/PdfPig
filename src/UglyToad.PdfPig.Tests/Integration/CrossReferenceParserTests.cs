namespace UglyToad.PdfPig.Tests.Integration
{
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
    }
}
