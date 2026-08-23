namespace UglyToad.PdfPig.Tests.Graphics.Colors.Icc
{
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Colors.Icc;
    using Xunit;

    /// <summary>
    /// Mapping a device colour onto the output intent profile's colour space, which is what decides whether
    /// a device colour can be managed at all.
    /// </summary>
    public class OutputIntentColorManagementTests
    {
        [Fact]
        public void MatchingComponentCount_PassesThrough()
        {
            bool ok = OutputIntentColorManagement.TryMapDeviceToProfileComponents(
                ColorSpace.DeviceCMYK, new[] { 0.1, 0.2, 0.3, 0.4 }, 4, out var mapped);

            Assert.True(ok);
            Assert.Equal(new[] { 0.1, 0.2, 0.3, 0.4 }, mapped.ToArray());
        }

        [Fact]
        public void DeviceGray_ToCmykProfile_MapsToBlackChannel()
        {
            // grey 0.25 -> K = 1 - 0.25 = 0.75, C = M = Y = 0.
            bool ok = OutputIntentColorManagement.TryMapDeviceToProfileComponents(
                ColorSpace.DeviceGray, new[] { 0.25 }, 4, out var mapped);

            Assert.True(ok);
            Assert.Equal(new[] { 0.0, 0.0, 0.0, 0.75 }, mapped.ToArray());
        }

        [Fact]
        public void DeviceGray_ToRgbProfile_Replicated()
        {
            bool ok = OutputIntentColorManagement.TryMapDeviceToProfileComponents(
                ColorSpace.DeviceGray, new[] { 0.25 }, 3, out var mapped);

            Assert.True(ok);
            Assert.Equal(new[] { 0.25, 0.25, 0.25 }, mapped.ToArray());
        }

        [Fact]
        public void DeviceGray_ToGrayProfile_PassesThrough()
        {
            bool ok = OutputIntentColorManagement.TryMapDeviceToProfileComponents(
                ColorSpace.DeviceGray, new[] { 0.25 }, 1, out var mapped);

            Assert.True(ok);
            Assert.Equal(new[] { 0.25 }, mapped.ToArray());
        }

        [Fact]
        public void DeviceRgb_ToCmykProfile_NotMapped()
        {
            // No well-defined neutral RGB -> CMYK mapping; left to the built-in conversion.
            bool ok = OutputIntentColorManagement.TryMapDeviceToProfileComponents(
                ColorSpace.DeviceRGB, new[] { 0.1, 0.2, 0.3 }, 4, out var mapped);

            Assert.False(ok);
            Assert.True(mapped.IsEmpty);
        }

        [Fact]
        public void DeviceCmyk_ToRgbProfile_NotMapped()
        {
            bool ok = OutputIntentColorManagement.TryMapDeviceToProfileComponents(
                ColorSpace.DeviceCMYK, new[] { 0.1, 0.2, 0.3, 0.4 }, 3, out var mapped);

            Assert.False(ok);
            Assert.True(mapped.IsEmpty);
        }
    }
}
