namespace UglyToad.PdfPig.Tests.Images
{
    using System;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Core;
    using PdfPig.Images;
    using Xunit;

    /// <summary>
    /// The Decode array a colour space implies when an image declares none (8.9.5.10, Table 89), and the
    /// byte encoding that rests on it.
    /// <para>
    /// A sample byte cannot hold an L* of 100 or a negative a*, so for a non-Indexed space it holds the
    /// component's <i>position</i> within that default range rather than its value; the colour space
    /// reverses the mapping in its Transform. Assuming the range is always [0, 1] clamped every Lab
    /// sample to 255 and rendered Lab imagery near-black.
    /// </para>
    /// </summary>
    public class DefaultDecodeTests
    {
        private static readonly double[] D50WhitePoint = [0.9505, 1.0, 1.089];

        private static double[] DefaultDecodeOf(ColorSpaceDetails details, int bitsPerComponent = 8)
        {
            var decode = new double[2 * details.NumberOfColorComponents];
            details.GetDefaultDecode(bitsPerComponent, decode);
            return decode;
        }

        [Fact]
        public void DeviceSpaces_DefaultDecodeIsUnitPerComponent()
        {
            Assert.Equal(new[] { 0.0, 1.0 }, DefaultDecodeOf(DeviceGrayColorSpaceDetails.Instance));
            Assert.Equal(new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, DefaultDecodeOf(DeviceRgbColorSpaceDetails.Instance));
            Assert.Equal(new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 },
                DefaultDecodeOf(DeviceCmykColorSpaceDetails.Instance));
        }

        [Fact]
        public void Lab_DefaultDecodeIsLStarTo100AndTheRangeEntry()
        {
            var lab = new LabColorSpaceDetails(D50WhitePoint, null, [-90.0, 90.0, -80.0, 80.0]);

            Assert.Equal(new[] { 0.0, 100.0, -90.0, 90.0, -80.0, 80.0 }, DefaultDecodeOf(lab));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 3)]
        [InlineData(4, 15)]
        [InlineData(8, 255)]
        public void Indexed_DefaultDecodeSpansTheIndexRange(int bitsPerComponent, int expectedMax)
        {
            // An Indexed sample is a palette index, not a colour, so its default Decode is the identity
            // at every bit depth.
            var indexed = new IndexedColorSpaceDetails(DeviceRgbColorSpaceDetails.Instance, 1,
                [0, 0, 0, 255, 255, 255]);

            Assert.Equal(new double[] { 0.0, expectedMax }, DefaultDecodeOf(indexed, bitsPerComponent));
        }

        [Fact]
        public void DeviceRgb8Bpc_WithNoDecodeArray_LeavesSamplesUntouched()
        {
            // The overwhelmingly common case, and the one the reworked ApplyDecode must not disturb.
            Span<byte> samples = stackalloc byte[6] { 0, 10, 128, 200, 255, 7 };
            byte[] expected = samples.ToArray();

            var result = ColorSpaceDetailsByteConverter.Convert(DeviceRgbColorSpaceDetails.Instance,
                samples, 8, 2, 1, null, RenderingIntent.RelativeColorimetric);

            Assert.Equal(expected, result.ToArray());
        }

        /// <summary>
        /// One Lab pixel through the full image pipeline: byte-level Decode, then the colour space's own
        /// Transform.
        /// </summary>
        private static (double R, double G, double B) LabPixelThroughImagePath(
            byte[] pixel, double[]? range = null, double[]? decode = null)
        {
            var lab = new LabColorSpaceDetails(D50WhitePoint, null, range);

            var rgb = ColorSpaceDetailsByteConverter.Convert(lab, pixel, 8, 1, 1, decode,
                RenderingIntent.RelativeColorimetric);

            return (rgb[0] / 255.0, rgb[1] / 255.0, rgb[2] / 255.0);
        }

        [Fact]
        public void LabImage_MaxLightnessSample_IsNearWhite()
        {
            // L* = 255/255 * 100 = 100, a* = b* = ~0. Treating the byte as a value in [0, 1] gave L* = 1
            // and rendered this near-black.
            var (r, g, b) = LabPixelThroughImagePath([0xFF, 0x80, 0x80]);

            Assert.True(r > 0.9 && g > 0.9 && b > 0.9, $"Expected near-white but got ({r}, {g}, {b}).");
        }

        [Fact]
        public void LabImage_ZeroLightnessSample_IsNearBlack()
        {
            var (r, g, b) = LabPixelThroughImagePath([0x00, 0x80, 0x80]);

            Assert.True(r < 0.1 && g < 0.1 && b < 0.1, $"Expected near-black but got ({r}, {g}, {b}).");
        }

        [Fact]
        public void LabImage_ExplicitDecodeEqualToTheDefault_MatchesHavingNoDecodeAtAll()
        {
            // [0 100 -100 100 -100 100] is what a Lab image usually writes, and it is exactly the default.
            // Recognising that is what keeps it off the rescaling path - where it used to be scaled by 255
            // and clamped.
            var withDecode = LabPixelThroughImagePath([0xFF, 0x80, 0x80],
                decode: [0.0, 100.0, -100.0, 100.0, -100.0, 100.0]);
            var withoutDecode = LabPixelThroughImagePath([0xFF, 0x80, 0x80]);

            Assert.Equal(withoutDecode.R, withDecode.R, 12);
            Assert.Equal(withoutDecode.G, withDecode.G, 12);
            Assert.Equal(withoutDecode.B, withDecode.B, 12);
        }

        [Fact]
        public void LabImage_ImagePathAgreesWithTheScalarPath()
        {
            // The round-trip the two paths have to share: a sample byte encodes a position within the
            // component's range, and GetColor is handed the value that position stands for. If the encoding
            // and the decoding disagree, the same colour renders differently as a fill and as an image.
            byte[] pixel = [0xC0, 0x40, 0xB0];
            var (ir, ig, ib) = LabPixelThroughImagePath(pixel);

            var lab = new LabColorSpaceDetails(D50WhitePoint, null, null);
            Span<double> components = stackalloc double[3];
            lab.DecodeRawComponents(pixel, components);
            var (sr, sg, sb) = lab.GetColor(components).ToRGBValues();

            // The image path quantises to a byte, so agreement is to within one 255th.
            Assert.Equal(sr, ir, 2);
            Assert.Equal(sg, ig, 2);
            Assert.Equal(sb, ib, 2);
        }

        [Fact]
        public void LabImage_DegenerateRangeDoesNotProduceNaN()
        {
            // /Range [0 0 0 0] is legal and pins a* and b* to a single value; the position of a sample
            // within a zero-width range is undefined, so it must resolve to that value rather than divide
            // by zero. A pinned a*/b* means a pure neutral grey.
            var (r, g, b) = LabPixelThroughImagePath([0x80, 0x40, 0xC0], range: [0.0, 0.0, 0.0, 0.0]);

            Assert.False(double.IsNaN(r) || double.IsNaN(g) || double.IsNaN(b));
            Assert.Equal(r, g, 2);
            Assert.Equal(g, b, 2);
        }

        [Fact]
        public void IndexedImage_SamplesRemainIndicesAtSubByteDepths()
        {
            // Indexed keeps its old contract: 4bpc samples unpack to indices in [0, 15] and are looked up,
            // not stretched to bytes.
            var indexed = new IndexedColorSpaceDetails(DeviceRgbColorSpaceDetails.Instance, 2,
                [0, 0, 0, 255, 0, 0, 0, 0, 255]);

            // Two 4-bit samples per byte: indices 1 and 2.
            Span<byte> packed = stackalloc byte[1] { 0x12 };
            var rgb = ColorSpaceDetailsByteConverter.Convert(indexed, packed, 4, 2, 1, null,
                RenderingIntent.RelativeColorimetric);

            Assert.Equal(6, rgb.Length);
            Assert.Equal(new byte[] { 255, 0, 0 }, rgb.Slice(0, 3).ToArray()); // index 1 -> red
            Assert.Equal(new byte[] { 0, 0, 255 }, rgb.Slice(3, 3).ToArray()); // index 2 -> blue
        }
    }
}
