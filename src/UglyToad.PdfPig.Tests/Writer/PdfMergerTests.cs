namespace UglyToad.PdfPig.Tests.Writer
{
    using Integration;
    using PdfPig.Writer;
    using System.IO;
    using Xunit;

    public class PdfMergerTests
    {
        [Fact]
        public void CanMerge2SimpleDocuments()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");

            var result = PdfMerger.Merge(one, two);

            using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(2, document.NumberOfPages);

                Assert.Equal(1.5m, document.Version);

                var page1 = document.GetPage(1);

                Assert.Equal("Write something inInkscape", page1.Text);

                var page2 = document.GetPage(2);

                Assert.Equal("I am a simple pdf.", page2.Text);
            }
        }

        [Fact]
        public void CanMerge2SimpleDocumentsReversed()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");

            var result = PdfMerger.Merge(one, two);

            System.IO.File.WriteAllBytes(@"C:\temp\merged.pdf", result);

            using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(2, document.NumberOfPages);

                Assert.Equal(1.5m, document.Version);

                var page1 = document.GetPage(1);

                Assert.Equal("I am a simple pdf.", page1.Text);

                var page2 = document.GetPage(2);

                Assert.Equal("Write something inInkscape", page2.Text);
            }
        }

        [Fact]
        public void RootNodePageCount()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");

            var result = PdfMerger.Merge(one, two);

            using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(2, document.NumberOfPages);
            }

            var oneBytes = File.ReadAllBytes(one);

            var result2 = PdfMerger.Merge(new[] { result, oneBytes });

            using (var document = PdfDocument.Open(result2, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(3, document.NumberOfPages);
            }
        }
    }
}
