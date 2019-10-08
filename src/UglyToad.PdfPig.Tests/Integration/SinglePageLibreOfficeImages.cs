namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Linq;
    using Xunit;

    public class SinglePageLibreOfficeImages
    {
        private static string GetFilePath() => IntegrationHelpers.GetDocumentPath(@"Single Page Images - from libre office.pdf");

        [Fact]
        public void Has3Images()
        {
            using (var document = PdfDocument.Open(GetFilePath(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);
                
                var images = page.GetImages().ToList();

                Assert.Equal(3, images.Count);
            }
        }

        [Fact]
        public void ImagesHaveCorrectDimensionsAndLocations()
        {
            using (var document = PdfDocument.Open(GetFilePath(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                var images = page.GetImages().OrderBy(x => x.Bounds.Width).ToList();

                var pdfPigSquare = images[0];

                Assert.Equal(148.3m, pdfPigSquare.Bounds.Width);
                Assert.Equal(148.3m, pdfPigSquare.Bounds.Height);
                Assert.Equal(60.1m, pdfPigSquare.Bounds.Left);
                Assert.Equal(765.8m, pdfPigSquare.Bounds.Top);

                var pdfPigSquished = images[1];

                Assert.Equal(206.8m, pdfPigSquished.Bounds.Width);
                Assert.Equal(83.2m, pdfPigSquished.Bounds.Height);
                Assert.Equal(309.8m, pdfPigSquished.Bounds.Left);
                Assert.Equal(552.1m, pdfPigSquished.Bounds.Top);

                var birthdayPigs = images[2];

                Assert.Equal(391m, birthdayPigs.Bounds.Width);
                Assert.Equal(267.1m, birthdayPigs.Bounds.Height);
                Assert.Equal(102.2m, birthdayPigs.Bounds.Left);
                Assert.Equal(426.3m, birthdayPigs.Bounds.Top);
            }
        }

        [Fact]
        public void HasCorrectText()
        {
            using (var document = PdfDocument.Open(GetFilePath(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);
                Assert.Equal("Oink oink", page.Text);
            }
        }

        [Fact]
        public void CanAccessImageBytesExceptUnsupported()
        {
            using (var document = PdfDocument.Open(GetFilePath(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);
                foreach (var image in page.GetImages())
                {
                    try
                    {
                        Assert.NotNull(image.Bytes);
                    }
                    catch (NotSupportedException )
                    {
                        // Should allow access to raw bytes.
                        Assert.NotNull(image.RawBytes);
                    }
                }
            }
        }
    }
}
