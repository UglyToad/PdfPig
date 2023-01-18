namespace UglyToad.PdfPig.Tests.Images
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Images.Png;
    using Xunit;

    public class PngFromPdfImageFactoryTests
    {
        private static readonly byte[] RgbBlack = new byte[] { 0, 0, 0 };
        private static readonly byte[] RgbWhite = new byte[] { 255, 255, 255 };
        private static readonly byte[][] RgbPalette = new[] { RgbBlack, RgbWhite };

        private static readonly byte[] CmykBlack = new byte[] { 0, 0, 0, 255 };
        private static readonly byte[] CmykWhite = new byte[] { 0, 0, 0, 0 };

        private static readonly byte GrayscaleBlack = 0;
        private static readonly byte GrayscaleWhite = 255;

        [Fact]
        public void CanGeneratePngFromDeviceRgbImageData()
        {
            var pixels = new[]
            {
                RgbWhite, RgbBlack, RgbWhite,
                RgbBlack, RgbWhite, RgbBlack,
                RgbWhite, RgbBlack, RgbWhite
            };

            var image = new TestPdfImage
            {
                ColorSpaceDetails = DeviceRgbColorSpaceDetails.Instance,
                DecodedBytes = pixels.SelectMany(b => b).ToArray(),
                WidthInSamples = 3,
                HeightInSamples = 3
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("3x3.png"), bytes));
        }

        [Fact]
        public void CanGeneratePngFromDeviceCMYKImageData()
        {
            var pixels = new[]
            {
                CmykWhite, CmykBlack, CmykWhite,
                CmykBlack, CmykWhite, CmykBlack,
                CmykWhite, CmykBlack, CmykWhite
            };

            var image = new TestPdfImage
            {
                ColorSpaceDetails = DeviceCmykColorSpaceDetails.Instance,
                DecodedBytes = pixels.SelectMany(b => b).ToArray(),
                WidthInSamples = 3,
                HeightInSamples = 3
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("3x3.png"), bytes));
        }

        [Fact]
        public void CanGeneratePngFromDeviceGrayscaleImageData()
        {
            var pixels = new[]
            {
                GrayscaleWhite, GrayscaleBlack, GrayscaleWhite,
                GrayscaleBlack, GrayscaleWhite, GrayscaleBlack,
                GrayscaleWhite, GrayscaleBlack, GrayscaleWhite
            };

            var image = new TestPdfImage
            {
                ColorSpaceDetails = DeviceGrayColorSpaceDetails.Instance,
                DecodedBytes = pixels,
                WidthInSamples = 3,
                HeightInSamples = 3
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("3x3.png"), bytes));
        }

        [Fact]
        public void CanGeneratePngFromIndexedImageData8bpc()
        {
            var indices = new byte[]
            {
                1, 0, 1,
                0, 1, 0,
                1, 0, 1
            };

            var image = new TestPdfImage
            {
                ColorSpaceDetails = new IndexedColorSpaceDetails(DeviceRgbColorSpaceDetails.Instance, 1, RgbPalette.SelectMany(b => b).ToArray()),
                DecodedBytes = indices,
                WidthInSamples = 3,
                HeightInSamples = 3
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("3x3.png"), bytes));
        }

        [Fact]
        public void CanGeneratePngFromIndexedImageData1bpc()
        {
            // Indices for a 3x3 RGB image, each index is represented by a single bit
            // 1, 0, 1, 
            // 0, 1, 0,
            // 1, 0, 1
            //
            // A scanline must be at least one byte wide, so the remaining five bits are padding:
            // Byte 0: 10100000  (bits #7 and #5 are 1)
            // Byte 1: 01000000  (bit #6 is 1)
            // Byte 2: 10100000  (bits #7 and #5 are 1)
            //         ||||||||
            // Bit # : 76543210

            var lines = new byte[3];
            lines[0] |= (1 << 7); // Set bit #7 to 1
            lines[0] |= (1 << 5); // Set bit #5 to 1

            lines[1] |= (1 << 6); // Set bit #6 to 1

            lines[2] |= (1 << 7); // Set bit #7 to 1
            lines[2] |= (1 << 5); // Set bit #5 to 1

            var colorTable = RgbPalette.SelectMany(b => b).ToArray();

            var image = new TestPdfImage
            {
                ColorSpaceDetails = new IndexedColorSpaceDetails(DeviceRgbColorSpaceDetails.Instance, 1, colorTable),
                DecodedBytes = lines,
                WidthInSamples = 3,
                HeightInSamples = 3,
                BitsPerComponent = 1
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("3x3.png"), bytes));
        }

        [Fact]
        public void CanGeneratePngFromCcittFaxDecodedImageData()
        {
            var decodedBytes = ImageHelpers.LoadFileBytes("ccittfax-decoded.bin");
            var image = new TestPdfImage
            {
                ColorSpaceDetails = IndexedColorSpaceDetails.Stencil(DeviceGrayColorSpaceDetails.Instance, new[] { 1m, 0 }),
                DecodedBytes = decodedBytes,
                WidthInSamples = 1800,
                HeightInSamples = 3113,
                BitsPerComponent = 1
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("ccittfax.png"), bytes));
        }

        [Fact]
        public void CanGeneratePngFromICCBasedImageData()
        {
            var decodedBytes = ImageHelpers.LoadFileBytes("iccbased-decoded.bin");
            var image = new TestPdfImage
            {
                ColorSpaceDetails = new ICCBasedColorSpaceDetails(
                    numberOfColorComponents: 3,
                    alternateColorSpaceDetails: DeviceRgbColorSpaceDetails.Instance,
                    range: new List<decimal> { 0, 1, 0, 1, 0, 1 },
                    metadata: null),
                DecodedBytes = decodedBytes,
                WidthInSamples = 1,
                HeightInSamples = 1,
                BitsPerComponent = 8
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("iccbased.png"), bytes));
        }

        [Fact]
        public void AlternateColorSpaceDetailsIsCurrentlyUsedInPdfPigWhenGeneratingPngsFromICCBasedImageData()
        {
            var decodedBytes = ImageHelpers.LoadFileBytes("iccbased-decoded.bin");
            var iccBasedImage = new TestPdfImage
            {
                ColorSpaceDetails = new ICCBasedColorSpaceDetails(
                    numberOfColorComponents: 3,
                    alternateColorSpaceDetails: DeviceRgbColorSpaceDetails.Instance,
                    range: new List<decimal> { 0, 1, 0, 1, 0, 1 },
                    metadata: null),
                DecodedBytes = decodedBytes,
                WidthInSamples = 1,
                HeightInSamples = 1,
                BitsPerComponent = 8
            };

            var deviceRGBImage = new TestPdfImage
            {
                ColorSpaceDetails = DeviceRgbColorSpaceDetails.Instance,
                DecodedBytes = decodedBytes,
                WidthInSamples = 1,
                HeightInSamples = 1,
                BitsPerComponent = 8
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(iccBasedImage, out var iccPngBytes));
            Assert.True(PngFromPdfImageFactory.TryGenerate(deviceRGBImage, out var deviceRgbBytes));
            Assert.Equal(iccPngBytes, deviceRgbBytes);

        }

        [Fact]
        public void CanGeneratePngFromCalRGBImageData()
        {
            var decodedBytes = ImageHelpers.LoadFileBytes("calrgb-decoded.bin");
            var image = new TestPdfImage
            {
                ColorSpaceDetails = new CalRGBColorSpaceDetails(
                    whitePoint: new List<decimal> { 0.95043m, 1, 1.09m },
                    blackPoint: null,
                    gamma: new List<decimal> { 2.2m, 2.2m, 2.2m },
                    matrix: new List<decimal> {
                        0.41239m, 0.21264m, 0.01933m,
                        0.35758m, 0.71517m, 0.11919m,
                        0.18045m, 0.07218m, 0.9504m }),
                DecodedBytes = decodedBytes,
                WidthInSamples = 153,
                HeightInSamples = 83,
                BitsPerComponent = 8
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("calrgb.png"), bytes));
        }

        [Fact]
        public void CanGeneratePngFromCalGrayImageData()
        {
            var decodedBytes = ImageHelpers.LoadFileBytes("calgray-decoded.bin", isCompressed: true);
            var image = new TestPdfImage
            {
                ColorSpaceDetails = new CalGrayColorSpaceDetails(
                    whitePoint: new List<decimal> { 0.9505000114m, 1, 1.0889999866m },
                    blackPoint: null,
                    gamma: 2.2000000477m),
                DecodedBytes = decodedBytes,
                WidthInSamples = 2480,
                HeightInSamples = 1748,
                BitsPerComponent = 8,
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("calgray.png"), bytes));
        }

        [Fact]
        public void CanGeneratePngFromJbig2DecodedImageData()
        {
            var decodedBytes = ImageHelpers.LoadFileBytes("sampledata_page1.jb2-decoded.bin");
            var image = new TestPdfImage
            {
                ColorSpaceDetails = DeviceGrayColorSpaceDetails.Instance,
                DecodedBytes = decodedBytes,
                WidthInSamples = 64,
                HeightInSamples = 56,
                BitsPerComponent = 1
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("sampledata_page1.jb2.png"), bytes));
        }

        private static byte[] LoadImage(string name)
        {
            return ImageHelpers.LoadFileBytes(name);
        }
    }
}
