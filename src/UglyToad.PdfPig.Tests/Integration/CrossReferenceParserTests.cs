namespace UglyToad.PdfPig.Tests.Integration
{
    public class CrossReferenceParserTests
    {
        [Fact]
        public void ParseMustNotFailWhenTryingToRecognizeUnexpectedStructures()
        {
            var pdf = IntegrationHelpers.GetSpecificTestDocumentPath("startxref-jpeg-stream");

            var doc = PdfDocument.Open(pdf);
            Assert.NotNull(doc);
        }
    }
}
