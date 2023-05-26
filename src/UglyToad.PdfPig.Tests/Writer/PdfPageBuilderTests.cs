namespace UglyToad.PdfPig.Tests.Images
{
    using System;
    using System.IO;
    using System.Linq;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Fonts.Standard14Fonts;
    using UglyToad.PdfPig.Writer;
    using Xunit;

    public class PdfPageBuilderTests
    {
        [Fact]
        public void CanAddPng()
        {
            byte[] pdfBytes;
            using (var pdfBuilder = new PdfDocumentBuilder())
            {
                pdfBuilder.AddStandard14Font(Standard14Font.Courier);
                {
                    var page1 = pdfBuilder.AddPage(595d, 842d); // A4
                    var dataPNG = LoadPng("1-16bitRGBA-Issue550.png");
                    page1.AddPng(dataPNG, new PdfRectangle(0, 0, 595, 842));
                }
                {
                    var page2 = pdfBuilder.AddPage(595d, 842d); // A4
                    var dataPNG = LoadPng("2-16bitRGB.png");
                    page2.AddPng(dataPNG, new PdfRectangle(0, 0, 595, 842));
                }
                {
                    var page3 = pdfBuilder.AddPage(595d, 842d); // A4
                    var dataPNG = LoadPng("3-16bitGray.png");
                    page3.AddPng(dataPNG, new PdfRectangle(0, 0, 595, 842));
                }

                {
                    var page4 = pdfBuilder.AddPage(595d, 842d); // A4
                    var dataPNG = LoadPng("4-16bitRGBA.png");
                    page4.AddPng(dataPNG, new PdfRectangle(0, 0, 595, 842));
                }
                pdfBytes = pdfBuilder.Build();
            }

            File.WriteAllBytes(@"PdfPageBuilderTests_CanAddPng.pdf", pdfBytes);

            using (var doc = PdfDocument.Open(pdfBytes))
            {
                var numberOfPages = doc.NumberOfPages;
                Assert.Equal(4, numberOfPages);

                // Page 1 - Image 1
                {
                    var page = doc.GetPage(1);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(1170, image1.WidthInSamples);
                    Assert.Equal(2532, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 2 - Image 2 
                {
                    var page = doc.GetPage(2);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(900, image1.WidthInSamples);
                    Assert.Equal(900, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 3 - Image 3 
                {
                    var page = doc.GetPage(3);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(900, image1.WidthInSamples);
                    Assert.Equal(900, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 4 - Image 4 
                {
                    var page = doc.GetPage(4);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(900, image1.WidthInSamples);
                    Assert.Equal(900, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }
            }
        }

        [Fact]
        public void CanAddPngTestPattern1()
        {
            const string subfolderName = "TestPattern1";
            byte[] pdfBytes;

            using (var pdfBuilder = new PdfDocumentBuilder())
            {
                var courierFont = pdfBuilder.AddStandard14Font(Standard14Font.Courier);

                AddPageWithImage(pdfBuilder, subfolderName, "tp1-001-8bitRGB-withExif~Thumbnail~ColorProfile.png", 150, courierFont);
                AddPageWithImage(pdfBuilder, subfolderName, "tp1-002-8bitRGBA-withExif~Thumbnail~ColorProfile.png", 200, courierFont);
                AddPageWithImage(pdfBuilder, subfolderName, "tp1-003-8bitRGB-Interlaced-withExif~ColorProfile.png", 150, courierFont);
                AddPageWithImage(pdfBuilder, subfolderName, "tp1-004-8bitRGBA-Interlaced-withExif~ColorProfile.png", 200, courierFont);

                AddPageWithImage(pdfBuilder, subfolderName, "tp1-101-16bitRGB-withExif~Thumbnail~ColorProfile.png", 150, courierFont);
                AddPageWithImage(pdfBuilder, subfolderName, "tp1-102-16bitRGBA-withExif~Thumbnail~ColorProfile.png", 200, courierFont);

                AddPageWithImage(pdfBuilder, subfolderName, "tp1-201-32bitRGB-withExif~Thumbnail~ColorProfile.png", 150, courierFont);

                AddPageWithImage(pdfBuilder, subfolderName, "tp1-301-16bitFloatRGB-withExif~Thumbnail~ColorProfile.png", 150, courierFont);

                AddPageWithImage(pdfBuilder, subfolderName, "tp1-401-32bitFloatRGB-withExif~Thumbnail~ColorProfile.png", 150, courierFont);

                pdfBytes = pdfBuilder.Build();
            }

            var outputFolder = Environment.CurrentDirectory;
            File.WriteAllBytes(@"PdfPageBuilderTests_CanAddPngTestPattern1.pdf", pdfBytes);

            using (var doc = PdfDocument.Open(pdfBytes))
            {
                var numberOfPages = doc.NumberOfPages;
                Assert.Equal(9, numberOfPages);

                // Page 1 - Image 1
                {
                    var page = doc.GetPage(6);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(200, image1.WidthInSamples);
                    Assert.Equal(200, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 2 - Image 2 
                {
                    var page = doc.GetPage(2);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(200, image1.WidthInSamples);
                    Assert.Equal(200, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 3 - Image 3 
                {
                    var page = doc.GetPage(3);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(200, image1.WidthInSamples);
                    Assert.Equal(150, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 4 - Image 4 
                {
                    var page = doc.GetPage(4);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(200, image1.WidthInSamples);
                    Assert.Equal(200, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 5 - Image 5 
                {
                    var page = doc.GetPage(5);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(200, image1.WidthInSamples);
                    Assert.Equal(150, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 6 - Image 6 
                {
                    var page = doc.GetPage(6);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(200, image1.WidthInSamples);
                    Assert.Equal(200, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 7 - Image 7 
                {
                    var page = doc.GetPage(7);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(200, image1.WidthInSamples);
                    Assert.Equal(150, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 8 - Image 8 
                {
                    var page = doc.GetPage(8);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(200, image1.WidthInSamples);
                    Assert.Equal(150, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }

                // Page 9 - Image 9 
                {
                    var page = doc.GetPage(9);
                    var image1 = page.GetImages().FirstOrDefault();

                    Assert.Equal(200, image1.WidthInSamples);
                    Assert.Equal(150, image1.HeightInSamples);
                    Assert.Equal(8, image1.BitsPerComponent);
                }
            }
        }

        private static void AddPageWithImage(PdfDocumentBuilder pdfBuilder, string subfolderName, string imageFileName, double imageHeight, PdfDocumentBuilder.AddedFont font)
        {
            var imageBottom = 842d - 600d;

            //var imagePlacement = new PdfRectangle(0, imageTop, 595d, imageTop - imageHeight);

            var imagePlacement = new PdfRectangle(0, imageBottom, 595d, 842d);
            var borderPlacement = new PdfPoint(imagePlacement.BottomLeft.X, imagePlacement.BottomLeft.Y);
            var labelPlacement = new PdfPoint(50, imagePlacement.BottomLeft.Y - 50);

            var page = pdfBuilder.AddPage(595d, 842d); // A4
            var dataPNG = LoadPng(imageFileName, subfolderName);
            page.DrawRectangle(borderPlacement, imagePlacement.Width, imagePlacement.Height, 3, true);
            page.AddPng(dataPNG, imagePlacement);
            page.AddText(imageFileName, 12, labelPlacement, font);
        }

        private static byte[] LoadPng(string name, string subfolderName = null)
        {
            var baseFolder = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            var PngFilesFolder = Path.Combine(baseFolder, "..", "..", "..", "Images", "Files", "Png");
            if (subfolderName is not null)
            {
                PngFilesFolder = Path.Combine(PngFilesFolder,subfolderName);
            }
            var PngFilePath = Path.Combine(PngFilesFolder, name);
            return File.ReadAllBytes(PngFilePath);
        }

        
    }
}
