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
    }
}
