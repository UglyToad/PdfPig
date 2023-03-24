namespace UglyToad.PdfPig.Tests.Integration
{
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
            var doubleComparer = new DoubleComparer(1);

            using (var document = PdfDocument.Open(GetFilePath(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                var images = page.GetImages().OrderBy(x => x.Bounds.Width).ToList();

                var pdfPigSquare = images[0];

                Assert.Equal(148.3d, pdfPigSquare.Bounds.Width, doubleComparer);
                Assert.Equal(148.3d, pdfPigSquare.Bounds.Height, doubleComparer);
                Assert.Equal(60.1d, pdfPigSquare.Bounds.Left, doubleComparer);
                Assert.Equal(765.8d, pdfPigSquare.Bounds.Top, doubleComparer);


                var pdfPigSquished = images[1];

                Assert.Equal(206.8d, pdfPigSquished.Bounds.Width, doubleComparer);
                Assert.Equal(83.2d, pdfPigSquished.Bounds.Height, doubleComparer);
                Assert.Equal(309.8d, pdfPigSquished.Bounds.Left, doubleComparer);
                Assert.Equal(552.1d, pdfPigSquished.Bounds.Top, doubleComparer);

                var birthdayPigs = images[2];

                Assert.Equal(391d, birthdayPigs.Bounds.Width, doubleComparer);
                Assert.Equal(267.1d, birthdayPigs.Bounds.Height, doubleComparer);
                Assert.Equal(102.2d, birthdayPigs.Bounds.Left, doubleComparer);
                Assert.Equal(426.3d, birthdayPigs.Bounds.Top, doubleComparer);
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
                var images = page.GetImages().ToArray();
                var numberOfImages = images.Length;
                for(int imageIndex=0; imageIndex<numberOfImages;imageIndex++)
                {

                    //TODO: Update test to attempt TryGetBytes on image 2 when ProgressDct implmented in DCTDecode.
                    var isDCTDecodeFilterSupporting = imageIndex != 2; // Image 2 uses ProgressDct not yet implemented.
                    var image = images[imageIndex];
                    if (isDCTDecodeFilterSupporting && image.TryGetBytes(out var bytes))
                    {
                        Assert.NotNull(bytes);
                    }
                    else
                    {
                        Assert.NotNull(image.RawBytes);
                    }
                }
            }
        }
    }
}
