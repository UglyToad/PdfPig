namespace UglyToad.PdfPig.Tests.Integration
{
    public class ShadingTests
    {
        [Fact]
        public void Issue702()
        {
            // Placeholder test for issue 702, the document contains a FunctionBasedShading
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("PDFBOX-1869-4-1.pdf")))
            {
                var page1 = document.GetPage(1);
            }
        }

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

        [Fact]
        public void AxialRadialTensorProductManyFunctions2()
        {
            // We just check pages can be parsed correctly for now
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("iron-ore-q2-q3-2013.pdf")))
            {
                var page = document.GetPage(8); // Should not throw
            }
        }
    }
}
