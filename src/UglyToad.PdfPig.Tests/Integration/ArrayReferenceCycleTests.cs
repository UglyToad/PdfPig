namespace UglyToad.PdfPig.Tests.Integration
{
    public class ArrayReferenceCycleTests
    {
        [Fact]
        public void CanGetPageWithImageResolvingIntoArrayReferenceCycle()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("array-reference-cycle.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);

                Assert.Single(page.GetImages());
            }
        }
    }
}
