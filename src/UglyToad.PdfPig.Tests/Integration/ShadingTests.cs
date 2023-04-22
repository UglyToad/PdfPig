namespace UglyToad.PdfPig.Tests.Integration
{
    using Xunit;

    public class ShadingTests
    {
        [Fact]
        public void AxialRadial1()
        {
            // We just check pages can be parsed correctly for now
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf")))
            {
                var page7 = document.GetPage(7);
                var page14 = document.GetPage(14);
                var page15 = document.GetPage(15);
                var page16 = document.GetPage(16);
                var page19 = document.GetPage(19);
            }
        }

        [Fact]
        public void AxialRadialTensorProduct1()
        {
            // We just check pages can be parsed correctly for now
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("MOZILLA-3136-0.pdf")))
            {
                for (int i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);
                }
            }
        }
    }
}
