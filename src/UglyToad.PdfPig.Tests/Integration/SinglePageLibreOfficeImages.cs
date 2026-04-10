namespace UglyToad.PdfPig.Tests.Integration
{
#if NET9_0_OR_GREATER
    using Microsoft.AspNetCore.WebUtilities;
#endif

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

        #if NET9_0_OR_GREATER
        [Fact]
        public void CanUseFileBufferingReadStream()
        {
            var bytes = File.ReadAllBytes(GetFilePath());

            using var mem = new MemoryStream(bytes);
            using var fbrs = new FileBufferingReadStream(mem, 256);

            using var doc = PdfDocument.Open(fbrs);

            var page = doc.GetPage(1);

            Assert.NotEmpty(page.Text);
        }
        #endif

        [Fact]
        public void ImagesHaveCorrectDimensionsAndLocations()
        {
            var doubleComparer = new DoubleComparer(0.1);

            using (var document = PdfDocument.Open(GetFilePath(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                var images = page.GetImages().OrderBy(x => x.BoundingBox.Width).ToList();

                var pdfPigSquare = images[0];

                Assert.Equal(148.3d, pdfPigSquare.BoundingBox.Width, doubleComparer);
                Assert.Equal(148.3d, pdfPigSquare.BoundingBox.Height, doubleComparer);
                Assert.Equal(60.1d, pdfPigSquare.BoundingBox.Left, doubleComparer);
                Assert.Equal(765.8d, pdfPigSquare.BoundingBox.Top, doubleComparer);


                var pdfPigSquished = images[1];

                Assert.Equal(206.8d, pdfPigSquished.BoundingBox.Width, doubleComparer);
                Assert.Equal(83.2d, pdfPigSquished.BoundingBox.Height, doubleComparer);
                Assert.Equal(309.8d, pdfPigSquished.BoundingBox.Left, doubleComparer);
                Assert.Equal(552.1d, pdfPigSquished.BoundingBox.Top, doubleComparer);

                var birthdayPigs = images[2];

                Assert.Equal(391d, birthdayPigs.BoundingBox.Width, doubleComparer);
                Assert.Equal(267.1d, birthdayPigs.BoundingBox.Height, doubleComparer);
                Assert.Equal(102.2d, birthdayPigs.BoundingBox.Left, doubleComparer);
                Assert.Equal(426.3d, birthdayPigs.BoundingBox.Top, doubleComparer);
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
                    if (image.TryGetBytesAsMemory(out var bytes))
                    {
                        Assert.False(bytes.IsEmpty);
                    }
                    else
                    {
                        Assert.False(image.RawMemory.IsEmpty);
                    }
                }
            }
        }
    }
}
