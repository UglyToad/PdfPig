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
