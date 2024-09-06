namespace UglyToad.PdfPig.Tests.Integration
{
    using SkiaSharp;
    using System.Linq;

    public class SoftMaskTests
    {
        [Fact]
        public void PigProductionHandbook()
        {
            var path = IntegrationHelpers.GetDocumentPath("Pig Production Handbook.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page = document.GetPage(1);

                var images = page.GetImages().ToArray();

                var image1 = images[1];
                Assert.NotNull(image1.SoftMaskImage);
                Assert.True(image1.TryGetPng(out var png1));
                using (var skImage1 = SKImage.FromEncodedData(png1))
                using (var skBitmap1 = SKBitmap.FromImage(skImage1))
                {
                    var pixel = skBitmap1.GetPixel(0, 0);
                    Assert.Equal(0, pixel.Alpha);
                }

                var image2 = images[2];
                Assert.NotNull(image2.SoftMaskImage);
                Assert.True(image2.TryGetPng(out var png2));
                using (var skImage2 = SKImage.FromEncodedData(png2))
                using (var skBitmap2 = SKBitmap.FromImage(skImage2))
                {
                    var pixel = skBitmap2.GetPixel(0, 0);
                    Assert.Equal(0, pixel.Alpha);
                }
            }
        }
    }
}
