using SkiaSharp;
using UglyToad.PdfPig.Rendering.Skia.Parser;

namespace UglyToad.PdfPig.Rendering.Skia.Tests
{
    public class RenderTests
    {
        // Image rotated bug

        const float scale = 1.5f;

        public RenderTests()
        {
            Directory.CreateDirectory("renders");
        }

        [Fact]
        public void PigProductionHandbook()
        {
            RenderDocument("Pig Production Handbook");
        }

        [Fact]
        public void d_68_1990_01_A()
        {
            RenderDocument("68-1990-01_A");
        }

        [Fact]
        public void d_22060_A1_01_Plans_1()
        {
            RenderDocument("22060_A1_01_Plans-1");
        }

        [Fact]
        public void cat_genetics_bobld()
        {
            RenderDocument("cat-genetics_bobld");
        }

        [Fact(Skip = "For debugging purpose")]
        public void RenderAllDocuments()
        {
            foreach (string doc in Helpers.GetAllDocuments())
            {
                string fileName = Path.GetFileNameWithoutExtension(doc);

                using (var document = PdfDocument.Open(doc))
                {
                    document.AddPageFactory<SKPicture, SkiaPageFactory>();

                    for (int p = 1; p <= document.NumberOfPages; p++)
                    {
                        var page = document.GetPage(p);

                        using (var picture = document.GetPage<SKPicture>(p))
                        {
                            Assert.NotNull(picture);

                            using (var fs = new FileStream($"renders\\{fileName}_{p}.png", FileMode.Create))
                            using (var image = SKImage.FromPicture(picture, new SKSizeI((int)(page.Width * scale), (int)(page.Height * scale)), SKMatrix.CreateScale(scale, scale)))
                            using (SKData d = image.Encode(SKEncodedImageFormat.Png, 100))
                            {
                                d.SaveTo(fs);
                            }
                        }
                    }
                }
            }
        }

        private static void RenderDocument(string path)
        {
            using (var document = PdfDocument.Open(Helpers.GetDocumentPath(path)))
            {
                document.AddPageFactory<SKPicture, SkiaPageFactory>();

                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);

                    using (var picture = document.GetPage<SKPicture>(p))
                    {
                        Assert.NotNull(picture);

                        using (var fs = new FileStream($"renders\\{path}_{p}.png", FileMode.Create))
                        using (var image = SKImage.FromPicture(picture, new SKSizeI((int)(page.Width * scale), (int)(page.Height * scale)), SKMatrix.CreateScale(scale, scale)))
                        using (SKData d = image.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            d.SaveTo(fs);
                        }
                    }
                }
            }
        }
    }
}
