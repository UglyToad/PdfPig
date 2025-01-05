namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;

    public class AdditionalGlyphListTests
    {
        [Fact]
        public void Type1FontSimple1()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("2108.11480")))
            {
                var page = document.GetPage(2);
                Assert.Contains("\u22c3", page.Letters.Select(l => l.Value));
            }
        }

        [Fact]
        public void Type1FontSimple2()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("ICML03-081")))
            {
                var page = document.GetPage(2);
                Assert.Contains("\u2211", page.Letters.Select(l => l.Value));
                Assert.Contains("\u220f", page.Letters.Select(l => l.Value));
                Assert.Contains("[", page.Letters.Select(l => l.Value));
                Assert.Contains("]", page.Letters.Select(l => l.Value));
            }
        }

        [Fact]
        public void Type1FontSimple3()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Math119FakingData")))
            {
                var page = document.GetPage(4);
                Assert.Contains("(", page.Letters.Select(l => l.Value));
                Assert.Contains(")", page.Letters.Select(l => l.Value));
                Assert.Contains("\u2211", page.Letters.Select(l => l.Value));
            }
        }
    }
}
