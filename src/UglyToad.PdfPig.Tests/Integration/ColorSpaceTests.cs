namespace UglyToad.PdfPig.Tests.Integration
{
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
    using UglyToad.PdfPig.Graphics.Colors;

    public class ColorSpaceTests
    {
        private const string OutputFolder = "ColorSpaceTests";

        public ColorSpaceTests()
        {
            Directory.CreateDirectory(OutputFolder);
        }

        [Fact]
        public void IndexedDeviceNColorSpaceImages()
        {
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-3136-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                // page 1
                var page1 = document.GetPage(1);
                var images1 = page1.GetImages().ToArray();

                // image 12
                var image12 = images1[12];
                Assert.Equal(ColorSpace.Indexed, image12.ColorSpaceDetails.Type);
                Assert.Equal(ColorSpace.DeviceN, image12.ColorSpaceDetails.BaseType);
                Assert.True(image12.TryGetPng(out byte[] bytes1_12)); // Cyan square
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-3136-0_1_12.png"), bytes1_12);

                // image 13
                var image13 = images1[13];
                Assert.Equal(ColorSpace.Indexed, image13.ColorSpaceDetails.Type);
                Assert.Equal(ColorSpace.DeviceN, image13.ColorSpaceDetails.BaseType);
                Assert.True(image13.TryGetPng(out byte[] bytes1_13)); // Cyan square
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-3136-0_1_13.png"), bytes1_13);
            }
        }

        [Fact]
        public void DeviceNColorSpaceImages()
        {
            var path = IntegrationHelpers.GetDocumentPath("DeviceN_CS_test.pdf");

            using (var document = PdfDocument.Open(path))
            {
                // page 3
                var page3 = document.GetPage(3);
                var images3 = page3.GetImages().ToArray();

                var image3_0 = images3[0];
                var deviceNCs = image3_0.ColorSpaceDetails as DeviceNColorSpaceDetails;
                Assert.NotNull(deviceNCs);
                Assert.True(deviceNCs.AlternateColorSpace is ICCBasedColorSpaceDetails);
                Assert.True(image3_0.TryGetPng(out byte[] bytes3_0));
                File.WriteAllBytes(Path.Combine(OutputFolder, "DeviceN_CS_test_3_0.png"), bytes3_0);

                var image3_2 = images3[2];
                deviceNCs = image3_2.ColorSpaceDetails as DeviceNColorSpaceDetails;
                Assert.NotNull(deviceNCs);
                Assert.True(deviceNCs.AlternateColorSpace is ICCBasedColorSpaceDetails);
                Assert.True(image3_2.TryGetPng(out byte[] bytes3_2));
                File.WriteAllBytes(Path.Combine(OutputFolder, "DeviceN_CS_test_3_2.png"), bytes3_2);

                // page 6
                var page6 = document.GetPage(6);
                var images6 = page6.GetImages().ToArray();

                var image6_0 = images6[0];
                deviceNCs = image6_0.ColorSpaceDetails as DeviceNColorSpaceDetails;
                Assert.NotNull(deviceNCs);
                Assert.True(deviceNCs.AlternateColorSpace is ICCBasedColorSpaceDetails);
                Assert.True(image6_0.TryGetPng(out byte[] bytes6_0));
                File.WriteAllBytes(Path.Combine(OutputFolder, "DeviceN_CS_test_6_0.png"), bytes6_0);

                var image6_1 = images6[1];
                deviceNCs = image6_0.ColorSpaceDetails as DeviceNColorSpaceDetails;
                Assert.NotNull(deviceNCs);
                Assert.True(deviceNCs.AlternateColorSpace is ICCBasedColorSpaceDetails);
                Assert.True(image6_1.TryGetPng(out byte[] bytes6_1));
                File.WriteAllBytes(Path.Combine(OutputFolder, "DeviceN_CS_test_6_1.png"), bytes6_1);

                var image6_2 = images6[2];
                deviceNCs = image6_2.ColorSpaceDetails as DeviceNColorSpaceDetails;
                Assert.NotNull(deviceNCs);
                Assert.True(deviceNCs.AlternateColorSpace is ICCBasedColorSpaceDetails);
                Assert.True(image6_2.TryGetPng(out byte[] bytes6_2));
                File.WriteAllBytes(Path.Combine(OutputFolder, "DeviceN_CS_test_6_2.png"), bytes6_2);
            }
        }

        [Fact]
        public void SeparationColorSpaceImages()
        {
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-7375-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page1 = document.GetPage(1);
                var images = page1.GetImages().ToArray();
                var image1page1 = images[0];
                var separationCs = image1page1.ColorSpaceDetails as SeparationColorSpaceDetails;
                Assert.NotNull(separationCs);
                Assert.True(separationCs.AlternateColorSpace is DeviceCmykColorSpaceDetails);

                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i].TryGetPng(out var png))
                    {
                        File.WriteAllBytes(Path.Combine(OutputFolder, $"MOZILLA-7375-0_1_{i}.png"), png);
                    }
                }
            }
        }

        [Fact]
        public void SeparationColorSpace()
        {
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-3136-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(4);
                var images = page.GetImages().ToArray();

                var image4 = images[4];

                var separation = image4.ColorSpaceDetails as SeparationColorSpaceDetails;
                Assert.NotNull(separation);

                Assert.True(image4.TryGetPng(out var png4));

                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-3136-0_4_separation.png"), png4);

                // Green dolphin image 
                // "Colorized TIFF (should appear only in GWG Green separation)"
                var image9 = images[9];

                var indexedCs = image9.ColorSpaceDetails as IndexedColorSpaceDetails;
                Assert.NotNull(indexedCs);
                Assert.Equal(ColorSpace.Separation, indexedCs.BaseColorSpace.Type);

                Assert.True(image9.TryGetPng(out var png9));

                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-3136-0_9_separation.png"), png9);
            }
        }

        [Fact]
        public void IndexedCalRgbColorSpaceImages()
        {
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-10084-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page1 = document.GetPage(1);
                var images1 = page1.GetImages().ToArray();

                var image0 = images1[0];
                Assert.Equal(ColorSpace.Indexed, image0.ColorSpaceDetails.Type);

                var indexedCs = image0.ColorSpaceDetails as IndexedColorSpaceDetails;
                Assert.NotNull(indexedCs);
                Assert.Equal(ColorSpace.CalRGB, indexedCs.BaseColorSpace.Type);
                Assert.True(image0.TryGetPng(out byte[] bytes0));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10084-0_1_0.png"), bytes0);

                var image1 = images1[1];
                Assert.Equal(ColorSpace.Indexed, image1.ColorSpaceDetails.Type);
                indexedCs = image1.ColorSpaceDetails as IndexedColorSpaceDetails;
                Assert.NotNull(indexedCs);
                Assert.Equal(ColorSpace.CalRGB, indexedCs.BaseColorSpace.Type);
                Assert.True(image1.TryGetPng(out byte[] bytes1));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10084-0_1_1.png"), bytes1);
            }
        }

        [Fact]
        public void StencilIndexedIccColorSpaceImages()
        {
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-10225-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                // page 1
                var page1 = document.GetPage(2);
                var images1 = page1.GetImages().ToArray();

                var image0 = images1[0];
                Assert.Equal(ColorSpace.Indexed, image0.ColorSpaceDetails.Type); // Icc
                Assert.True(image0.TryGetPng(out byte[] bytes0));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10225-0_1_0.png"), bytes0);

                var image1 = images1[1];
                Assert.Equal(ColorSpace.Indexed, image1.ColorSpaceDetails.Type); // stencil
                Assert.True(image1.TryGetPng(out byte[] bytes1));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10225-0_1_1.png"), bytes1);

                // page 23
                var page23 = document.GetPage(23);
                var images23 = page23.GetImages().ToArray();

                var image23_0 = images23[0];
                Assert.Equal(ColorSpace.Indexed, image23_0.ColorSpaceDetails.Type);
                Assert.True(image23_0.TryGetPng(out byte[] bytes23_0));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10225-0_23_0.png"), bytes23_0);

                // page 332
                var page332 = document.GetPage(332);
                var images332 = page332.GetImages().ToArray();

                var image332_0 = images332[0];
                Assert.Equal(ColorSpace.ICCBased, image332_0.ColorSpaceDetails.Type);
                Assert.True(image332_0.TryGetPng(out byte[] bytes332_0));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10225-0_332_0.png"), bytes332_0);

                // page 338
                var page338 = document.GetPage(338);
                var images338 = page338.GetImages().ToArray();

                var image338_1 = images338[1];
                Assert.Equal(ColorSpace.Indexed, image338_1.ColorSpaceDetails.Type);
                Assert.True(image338_1.TryGetPng(out byte[] bytes338_1));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10225-0_338_1.png"), bytes338_1);

                // page 339
                var page339 = document.GetPage(339);
                var images339 = page339.GetImages().ToArray();

                var image339_0 = images339[0];
                Assert.Equal(ColorSpace.Indexed, image339_0.ColorSpaceDetails.Type);
                Assert.True(image339_0.TryGetPng(out byte[] bytes339_0));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10225-0_339_0.png"), bytes339_0);

                var image339_1 = images339[1];
                Assert.Equal(ColorSpace.Indexed, image339_1.ColorSpaceDetails.Type);
                Assert.True(image339_1.TryGetPng(out byte[] bytes339_1));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10225-0_339_1.png"), bytes339_1);

                // page 341
                var page341 = document.GetPage(341);
                var images341 = page341.GetImages().ToArray();

                var image341_0 = images341[0];
                Assert.Equal(ColorSpace.Indexed, image341_0.ColorSpaceDetails.Type);
                Assert.True(image341_0.TryGetPng(out byte[] bytes341_0));
                File.WriteAllBytes(Path.Combine(OutputFolder, "MOZILLA-10225-0_341_0.png"), bytes341_0);
            }
        }
        
        [Fact]
        public void SeparationLabColorSpace()
        {
            // Test with TIKA_1552_0.pdf
            // https://icolorpalette.com/color/pantone-289-c
            // Pantone 289 C Color | #0C2340
            // Rgb : rgb(12,35,64)
            // CIE L*a*b* : 13.53, 2.89, -21.08

            var path = IntegrationHelpers.GetDocumentPath("TIKA-1552-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page1 = document.GetPage(1);

                var background = page1.ExperimentalAccess.Paths[0];
                Assert.True(background.IsFilled);

                var (r, g, b) = background.FillColor.ToRGBValues();

                // Colors picked from Acrobat reader: rgb(11, 34, 64)
                Assert.Equal(10, ConvertToByte(r)); // Should be 11, but close enough
                Assert.Equal(34, ConvertToByte(g));
                Assert.Equal(64, ConvertToByte(b));
            }
        }

        [Fact]
        public void CanGetAllPagesImages()
        {
            var path = IntegrationHelpers.GetDocumentPath("Pig Production Handbook.pdf");

            using (var document = PdfDocument.Open(path))
            {
                for (int p = 0; p < document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p + 1);
                    var images = page.GetImages().ToArray();
                    for (int i = 0; i < images.Length; i++)
                    {
                        if (images[i].TryGetPng(out var png))
                        {
                            File.WriteAllBytes(Path.Combine(OutputFolder, $"Pig Production Handbook_{p + 1}_{i}.png"), png);
                        }
                    }
                }
            }
        }

        [Fact]
        public void SeparationIccColorSpacesWithForm()
        {
            var path = IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf");

            using (var document = PdfDocument.Open(path))
            {
                Page page1 = document.GetPage(1);
                var paths1 = page1.ExperimentalAccess.Paths.Where(p => p.IsFilled).ToArray();
                var reflexRed = paths1[0].FillColor.ToRGBValues(); // 'Reflex Red' Separation color space
                Assert.Equal(0.930496, reflexRed.r, 6);
                Assert.Equal(0.111542, reflexRed.g, 6);
                Assert.Equal(0.142197, reflexRed.b, 6);

                Page page2 = document.GetPage(2);
                var words = page2.GetWords(NearestNeighbourWordExtractor.Instance).ToArray();

                var urlWord = words.First(w => w.ToString().Contains("www.extron.com"));
                var firstLetter = urlWord.Letters[0];
                Assert.Equal("w", firstLetter.Value);
                Assert.Equal((0, 0, 1), firstLetter.Color.ToRGBValues()); // Blue

                var paths2 = page2.ExperimentalAccess.Paths;
                var filledPath = paths2.Where(p => p.IsFilled).ToArray();
                var filledRects = filledPath.Where(p => p.Count == 1 && p[0].IsDrawnAsRectangle).ToArray();

                // Colors picked from Acrobat reader
                (double r, double g, double b) lightRed = (0.985, 0.942, 0.921);
                (double r, double g, double b) lightRed2 = (1, 0.95, 0.95);
                (double r, double g, double b) lightOrange = (0.993, 0.964, 0.929);

                var filledColors = filledRects
                    .OrderBy(x => x.GetBoundingRectangle().Value.Left)
                    .ThenByDescending(x => x.GetBoundingRectangle().Value.Top)
                    .Select(x => x.FillColor).ToArray();

                for (int r = 0; r < filledColors.Length; r++)
                {
                    var color = filledColors[r];
                    Assert.Equal(ColorSpace.DeviceRGB, color.ColorSpace);

                    if (r % 2 == 0)
                    {
                        if (r == 2)
                        {
                            Assert.Equal(lightRed2, color.ToRGBValues());
                        }
                        else
                        {
                            Assert.Equal(lightRed, color.ToRGBValues());
                        }
                    }
                    else
                    {
                        Assert.Equal(lightOrange, color.ToRGBValues());
                    }
                }
            }
        }

        [Fact]
        public void Issue724()
        {
            // 11194059_2017-11_de_s
            var path = IntegrationHelpers.GetDocumentPath("11194059_2017-11_de_s.pdf");
            using (var document = PdfDocument.Open(path))
            {
                // Should not throw an exception.
                // Fixed an issue in the Type 4 function Copy() StackOperators
                Page page1 = document.GetPage(1);
                Assert.NotNull(page1);

                Page page2 = document.GetPage(2);
                Assert.NotNull(page2);
            }
        }

        private static byte ConvertToByte(double componentValue)
        {
            var rounded = Math.Round(componentValue * 255, MidpointRounding.AwayFromZero);
            return (byte)rounded;
        }
    }
}
