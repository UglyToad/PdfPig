using UglyToad.PdfPig.Tests.Integration;
using Xunit;

namespace UglyToad.PdfPig.Tests.Geometry
{
    public class ClippingTests
    {
        [Fact]
        public void ContainsRectangleEvenOdd()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("SPARC - v9 Architecture Manual"),
                new ParsingOptions { ClipPaths = true }))
            {
                var page = document.GetPage(45);
                Assert.Equal(28, page.ExperimentalAccess.Paths.Count);
            }
        }
    }
}
