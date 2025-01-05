namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;

    public class ZapfDingbatsTests
    {
        [Fact]
        public void Type1Standard14Font1()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("TIKA-469-0")))
            {
                var page = document.GetPage(2);
                Assert.Contains("●", page.Letters.Select(l => l.Value));
            }
        }

        [Fact]
        public void Type1Standard14Font2()
        {
            // This document does not actually contain circular references
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("MOZILLA-LINK-5251-1")))
            {
                var page = document.GetPage(1);
                Assert.Contains("✁", page.Letters.Select(l => l.Value));
                Assert.Contains("✂", page.Letters.Select(l => l.Value));
                Assert.Contains("✄", page.Letters.Select(l => l.Value));
                Assert.Contains("☎", page.Letters.Select(l => l.Value));
                Assert.Contains("✆", page.Letters.Select(l => l.Value));
                Assert.Contains("✇", page.Letters.Select(l => l.Value));
            }
        }

        [Fact]
        public void Type1FontSimple1()
        {
            // This document does not actually contain circular references
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("MOZILLA-2775-1")))
            {
                var page = document.GetPage(11);
                Assert.Contains("●", page.Letters.Select(l => l.Value));
            }
        }

        [Fact]
        public void Type1FontSimple2()
        {
            // This document does not actually contain circular references
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("PDFBOX-492-4.jar-8")))
            {
                var page = document.GetPage(1);
                Assert.Contains("\u25a0", page.Letters.Select(l => l.Value));
            }
        }
    }
}
