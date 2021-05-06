namespace UglyToad.PdfPig.Tests.Images
{
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
                ColorSpaceDetails = IndexedColorSpaceDetails.CCITTFaxColorSpaceDetails,
                DecodedBytes = decodedBytes,
                WidthInSamples = 1800,
                HeightInSamples = 3113,
                BitsPerComponent = 1
            };

            Assert.True(PngFromPdfImageFactory.TryGenerate(image, out var bytes));
            Assert.True(ImageHelpers.ImagesAreEqual(LoadImage("ccittfax.png"), bytes));
        }

        private static byte[] LoadImage(string name)
        {
            return ImageHelpers.LoadFileBytes(name);
        }
    }
}
