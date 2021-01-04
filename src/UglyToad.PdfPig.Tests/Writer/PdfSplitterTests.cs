namespace UglyToad.PdfPig.Tests.Writer
{
    using Integration;
    using PdfPig.Writer;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class PdfSplitterTests
    {
        [Fact]
        public void CanSplitDocumentsInPages()
        {
            var one = IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf");

            var outputStreams = Enumerable.Range(0, 9).Select(r => new MemoryStream()).ToList();
            using (var input = File.OpenRead(one))
            {
                PdfSplitter.SplitEveryPage(input, outputStreams, 5);
            }

            foreach (var outputStream in outputStreams)
            {
                outputStream.Position = 0;
                var document = PdfDocument.Open(outputStream);

                Assert.Equal(5, document.NumberOfPages);
            }
        }

        [Fact]
        public void CanSplitDocumentsInTwo()
        {
            var one = IntegrationHelpers.GetDocumentPath("Pig Production Handbook.pdf");

            using (var output1 = new MemoryStream())
            {
                using (var output2 = new MemoryStream())
                {
                    using (var input = File.OpenRead(one))
                    {
                        PdfSplitter.SplitTwoParts(input, 7, output1, output2);
                    }

                    output1.Position = 0;
                    var document1 = PdfDocument.Open(output1);
                    Assert.Equal(6, document1.NumberOfPages);

                    output2.Position = 0;
                    var document2 = PdfDocument.Open(output2);
                    Assert.Equal(80, document2.NumberOfPages);
                }
            }
        }
    }
}
