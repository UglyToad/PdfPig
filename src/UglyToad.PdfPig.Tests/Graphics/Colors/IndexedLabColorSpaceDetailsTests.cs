namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Core;

    /// <summary>
    /// Indexed colour-table bytes must decode to the base colour space's native component
    /// ranges (ISO 32000-2, 8.6.6.3). Lab is the space where this matters: L* is [0, 100] and
    /// a*/b* come from the /Range entry, so decoding table bytes to [0, 1] (correct for device
    /// spaces) renders Lab entries near-black. These tests cover all three decode entry points:
    /// GetColor, GetRgb and Process.
    /// </summary>
    public class IndexedLabColorSpaceDetailsTests
    {
        private static readonly double[] D50WhitePoint = [0.9505, 1.0, 1.089];

        /// <summary>
        /// Two-entry table over Lab (default /Range [-100 100 -100 100]):
        /// index 0 = black (L*=0, a*=b*=~0), index 1 = white (L*=100, a*=b*=~0).
        /// A byte of 0x80 decodes to -100 + (128/255)*200 = ~0.4 on the a*/b* axes.
        /// </summary>
        private static IndexedColorSpaceDetails CreateIndexedOverLab(double[]? range = null)
        {
            var lab = new LabColorSpaceDetails(D50WhitePoint, null, range);
            return new IndexedColorSpaceDetails(lab, 1, [0x00, 0x80, 0x80, 0xFF, 0x80, 0x80]);
        }

        [Fact]
        public void GetColor_WhiteLabTableEntry_IsNearWhite()
        {
            var indexed = CreateIndexedOverLab();

            var (r, g, b) = indexed.GetColor([1]).ToRGBValues();

            // Without range decoding L* becomes 1.0 (of 100) and this renders near-black.
            Assert.True(r > 0.9 && g > 0.9 && b > 0.9, $"Expected near-white but got ({r}, {g}, {b}).");
        }

        [Fact]
        public void GetColor_BlackLabTableEntry_IsNearBlack()
        {
            var indexed = CreateIndexedOverLab();

            var (r, g, b) = indexed.GetColor([0]).ToRGBValues();

            Assert.True(r < 0.1 && g < 0.1 && b < 0.1, $"Expected near-black but got ({r}, {g}, {b}).");
        }

        [Fact]
        public void GetRgb_WhiteLabTableEntry_IsNearWhite()
        {
            var indexed = CreateIndexedOverLab();

            indexed.GetRgb([1.0], out double r, out double g, out double b);

            Assert.True(r > 0.9 && g > 0.9 && b > 0.9, $"Expected near-white but got ({r}, {g}, {b}).");
        }

        [Fact]
        public void Process_WhiteLabTableEntry_IsNearWhite()
        {
            var indexed = CreateIndexedOverLab();

            double[] rgb = indexed.Process([1], RenderingIntent.RelativeColorimetric);

            Assert.Equal(3, rgb.Length);
            Assert.True(rgb[0] > 0.9 && rgb[1] > 0.9 && rgb[2] > 0.9,
                $"Expected near-white but got ({rgb[0]}, {rgb[1]}, {rgb[2]}).");
        }

        [Fact]
        public void AllThreePaths_DecodeIdentically()
        {
            var indexed = CreateIndexedOverLab();

            var (cr, cg, cb) = indexed.GetColor([1]).ToRGBValues();
            indexed.GetRgb([1.0], out double rr, out double rg, out double rb);
            double[] p = indexed.Process([1], RenderingIntent.RelativeColorimetric);

            Assert.Equal(cr, rr, 12);
            Assert.Equal(cg, rg, 12);
            Assert.Equal(cb, rb, 12);
            Assert.Equal(cr, p[0], 12);
            Assert.Equal(cg, p[1], 12);
            Assert.Equal(cb, p[2], 12);
        }

        [Fact]
        public void GetColor_HonoursCustomRangeEntry()
        {
            // With /Range [0 0 0 0] the a*/b* axes are pinned to zero, so a mid-grey table
            // entry decodes to a pure neutral grey: r = g = b exactly (any a*/b* tint would
            // break the equality).
            var lab = new LabColorSpaceDetails(D50WhitePoint, null, [0.0, 0.0, 0.0, 0.0]);
            var indexed = new IndexedColorSpaceDetails(lab, 0, [0x80, 0x00, 0xFF]);

            var (r, g, b) = indexed.GetColor([0]).ToRGBValues();

            Assert.Equal(r, g, 12);
            Assert.Equal(g, b, 12);
            Assert.InRange(r, 0.3, 0.7);
        }
    }
}