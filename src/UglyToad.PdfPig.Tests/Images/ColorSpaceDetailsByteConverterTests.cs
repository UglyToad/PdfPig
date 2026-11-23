namespace UglyToad.PdfPig.Tests.Images
{
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Core;
    using PdfPig.Images;
    using PdfPig.Tests.Integration;

    /// <summary>
    /// <see cref="ColorSpaceDetailsByteConverter"/> is given the bytes from <see cref="Content.IPdfImage.TryGetBytesAsMemory"/>,
    /// which an image hands out again on every call, so converting them must leave them as they are.
    /// </summary>
    public class ColorSpaceDetailsByteConverterTests
    {
        [Fact]
        public void SixteenBitSamplesLeaveTheInputAsItIs()
        {
            AssertConverts(DeviceGrayColorSpaceDetails.Instance, [0x12, 0x34, 0xAB, 0xCD], 16, 2, 1, null, [0x12, 0xAB]);
        }

        [Fact]
        public void StridePaddingLeavesTheInputAsItIs()
        {
            // Rows of three bytes for an image two pixels wide.
            AssertConverts(DeviceGrayColorSpaceDetails.Instance, [1, 2, 9, 3, 4, 9], 8, 2, 2, null, [1, 2, 3, 4]);
        }

        [Fact]
        public void DecodeArrayLeavesTheInputAsItIs()
        {
            AssertConverts(DeviceGrayColorSpaceDetails.Instance, [0, 64, 128, 255], 8, 4, 1, [1.0, 0.0], [255, 191, 127, 0]);
        }

        [Fact]
        public void IndexedOnOneComponentLeavesTheInputAsItIs()
        {
            // The last index is above hival, so it is read as hival.
            var indexed = new IndexedColorSpaceDetails(DeviceGrayColorSpaceDetails.Instance, 2, [10, 20, 30]);

            AssertConverts(indexed, [0, 1, 2, 7], 8, 4, 1, null, [10, 20, 30, 30]);
        }

        [Fact]
        public void IndexAboveHivalLeavesTheInputAsItIs()
        {
            var indexed = new IndexedColorSpaceDetails(DeviceRgbColorSpaceDetails.Instance, 1, [0, 0, 0, 255, 255, 255]);

            AssertConverts(indexed, [0, 1, 5], 8, 3, 1, null, [0, 0, 0, 255, 255, 255, 255, 255, 255]);
        }

        [Fact]
        public void UnpackedSamplesWithStridePadding()
        {
            // Four bits a sample and rows padded to whole bytes, so after unpacking each row has a fourth sample
            // to drop. The samples are in a buffer of the converter's own by then.
            AssertConverts(DeviceGrayColorSpaceDetails.Instance, [0x12, 0x30, 0x45, 0x60], 4, 3, 2, null, [17, 34, 51, 68, 85, 102]);
        }

        [Fact]
        public void UnpackedIndexedSamples()
        {
            // A one-bit scan stored as Indexed on DeviceGray. The lookup works in the unpacked samples.
            var indexed = new IndexedColorSpaceDetails(DeviceGrayColorSpaceDetails.Instance, 1, [0, 255]);

            AssertConverts(indexed, [0b1010_0000, 0b0100_0000], 1, 3, 2, null, [255, 0, 255, 0, 255, 0]);
        }

        [Theory]
        [InlineData("MOZILLA-3136-0.pdf", 1)] // Indexed on a DeviceN with one colorant
        [InlineData("MOZILLA-3136-0.pdf", 3)] // 16 bits a component, and Indexed on DeviceGray
        [InlineData("TIKA-1552-0.pdf", 241)] // DeviceGray with /Decode [1 0]
        public void TryGetPngLeavesTheImageBytesAsTheyAre(string document, int pageNumber)
        {
            AssertTryGetPngLeavesTheImageBytes(IntegrationHelpers.GetDocumentPath(document), pageNumber);
        }

        [Fact]
        public void TryGetPngLeavesTheImageBytesAsTheyAreWithStridePadding()
        {
            AssertTryGetPngLeavesTheImageBytes(IntegrationHelpers.GetSpecificTestDocumentPath("FailedToParseContentForPage32.pdf"), 17);
        }

        private static void AssertConverts(ColorSpaceDetails details, byte[] input, int bitsPerComponent, int width, int height,
            double[]? decode, byte[] expected)
        {
            var original = input.ToArray();

            var first = ColorSpaceDetailsByteConverter.Convert(details, input, bitsPerComponent, width, height, decode,
                RenderingIntent.RelativeColorimetric).ToArray();

            Assert.Equal(original, input);
            Assert.Equal(expected, first);

            var second = ColorSpaceDetailsByteConverter.Convert(details, input, bitsPerComponent, width, height, decode,
                RenderingIntent.RelativeColorimetric).ToArray();

            Assert.Equal(expected, second);
        }

        private static void AssertTryGetPngLeavesTheImageBytes(string path, int pageNumber)
        {
            using var document = PdfDocument.Open(path, new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true });

            var pngs = 0;
            foreach (var image in document.GetPage(pageNumber).GetImages())
            {
                if (!image.TryGetBytesAsMemory(out var memory))
                {
                    continue;
                }

                var bytes = memory.ToArray();

                var hasPng = image.TryGetPng(out var first);

                Assert.True(image.TryGetBytesAsMemory(out var after));
                Assert.Equal(bytes, after.ToArray());

                Assert.Equal(hasPng, image.TryGetPng(out var second));
                Assert.Equal(first, second);

                pngs += hasPng ? 1 : 0;
            }

            Assert.True(pngs > 0);
        }
    }
}
