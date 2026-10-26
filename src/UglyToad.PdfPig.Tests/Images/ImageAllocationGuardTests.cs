namespace UglyToad.PdfPig.Tests.Images
{
    using System;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Images;
    using UglyToad.PdfPig.Images.Png;

    public class ImageAllocationGuardTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(16)]
        public void RejectsShortImageDataBeforeAllocatingFromDimensions(int bitsPerComponent)
        {
            Assert.Throws<ArgumentException>(() =>
                ColorSpaceDetailsByteConverter.Convert(DeviceGrayColorSpaceDetails.Instance,
                    new byte[1], bitsPerComponent, 4096, 4096));
        }

        [Fact]
        public void KeepsPreviouslySupportedShortImageDataFromPdfBox492()
        {
            // Public regression document supplied in the review of #1478:
            // https://github.com/user-attachments/files/33594010/PDFBOX-492-4.jar-8.pdf
            var path = Integration.IntegrationHelpers.GetSpecificTestDocumentPath("PDFBOX-492-4.jar-8.pdf");
            using var document = PdfDocument.Open(path);
            var image = Assert.Single(document.GetPage(2).GetImages());
            Assert.Equal(838, image.WidthInSamples);
            Assert.Equal(830, image.HeightInSamples);
            Assert.True(image.TryGetBytesAsMemory(out var decoded));
            Assert.InRange(decoded.Length, 1, image.WidthInSamples * image.HeightInSamples - 1);

            // Flate recovery differs by runtime. Preserve the old result where its row reads fit;
            // reject data that would also have failed the original row copies.
            var stride = decoded.Length / image.HeightInSamples;
            var sourceLength = (image.HeightInSamples - 1) * stride + image.WidthInSamples;
            if (sourceLength > decoded.Length)
            {
                Assert.Throws<ArgumentException>(() => ColorSpaceDetailsByteConverter.Convert(
                    image.ColorSpaceDetails, decoded.ToArray(), image.BitsPerComponent,
                    image.WidthInSamples, image.HeightInSamples, image.Decode, image.RenderingIntent));
                Assert.False(image.TryGetPng(out _));
                return;
            }

            var converted = ColorSpaceDetailsByteConverter.Convert(image.ColorSpaceDetails,
                decoded.ToArray(), image.BitsPerComponent, image.WidthInSamples, image.HeightInSamples,
                image.Decode, image.RenderingIntent);
            Assert.Equal(838 * 830 * 3, converted.Length);
            if (decoded.Length == 537592)
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                // RGB bytes produced by the original converter for the .NET 9 recovered input.
                Assert.Equal("AD118FD718FB29582577F7A31500117FBF2AAD4D8418FC66E685D5B5720ECE37",
                    BitConverter.ToString(sha256.ComputeHash(converted.ToArray())).Replace("-", ""));
            }
            Assert.True(image.TryGetPng(out _));
        }

        [Fact]
        public void KeepsOriginalPixelsWhenShortRowsNeedALargerBuffer()
        {
            var converted = ColorSpaceDetailsByteConverter.Convert(DeviceGrayColorSpaceDetails.Instance,
                new byte[] { 10, 20, 30, 40, 50 }, 8, 2, 3);
            Assert.Equal(new byte[] { 10, 20, 20, 30, 30, 40 }, converted.ToArray());
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 1)]
        public void RejectsInvalidDimensions(int width, int height)
        {
            Assert.Throws<ArgumentException>(() =>
                ColorSpaceDetailsByteConverter.Convert(DeviceGrayColorSpaceDetails.Instance,
                    new byte[1], 8, width, height));
            Assert.Throws<ArgumentException>(() => PngBuilder.Create(width, height, false));
        }

        [Fact]
        public void ChecksPngBufferLengthWithoutIntegerWraparound()
        {
            Assert.Throws<DecodeBuffer.LimitExceededException>(() => PngBuilder.Create(32768, 32768, false));
            Assert.Throws<OverflowException>(() => PngBuilder.Create(int.MaxValue, int.MaxValue, true));
        }

        [Fact]
        public void KeepsPackedScanlinePadding()
        {
            var result = ColorSpaceDetailsByteConverter.Convert(DeviceGrayColorSpaceDetails.Instance,
                new byte[] { 0xa0, 0x40 }, 1, 3, 2);
            Assert.Equal(new byte[] { 255, 0, 255, 0, 255, 0 }, result.ToArray());
        }

        [Fact]
        public void RejectsEmptyPngInputBeforeCreatingAnImageBuffer()
        {
            var image = new TestPdfImage
            {
                ColorSpaceDetails = DeviceGrayColorSpaceDetails.Instance,
                DecodedBytes = Array.Empty<byte>(),
                WidthInSamples = 4096,
                HeightInSamples = 4096
            };
#if NET
            var before = GC.GetAllocatedBytesForCurrentThread();
#endif
            Assert.False(PngFromPdfImageFactory.TryGenerate(image, out _));
#if NET
            Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 1024 * 1024);
#endif
        }
    }
}
