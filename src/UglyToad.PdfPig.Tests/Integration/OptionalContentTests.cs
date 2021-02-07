using Xunit;

namespace UglyToad.PdfPig.Tests.Integration
{
    public class OptionalContentTests
    {
        [Fact]
        public void MarkedOptionalContent()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("odwriteex.pdf")))
            {
                var page = document.GetPage(1);
                var oc = page.GetOptionalContents();

                Assert.Equal(3, oc.Count);

                Assert.Contains("0", oc);
                Assert.Contains("Dimentions", oc);
                Assert.Contains("Text", oc);

                Assert.Equal(1, oc["0"].Count);
                Assert.Equal(2, oc["Dimentions"].Count);
                Assert.Equal(1, oc["Text"].Count);
            }
        }
    }
}
