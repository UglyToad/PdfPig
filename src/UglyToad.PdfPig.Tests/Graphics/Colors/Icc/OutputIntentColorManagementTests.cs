namespace UglyToad.PdfPig.Tests.Graphics.Colors.Icc
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Core;
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
        /// <summary>
        /// Answers each pixel with the profile-space components it was handed - red takes the last
        /// component, green the first, blue the second - so a test can read the expansion straight off the
        /// output instead of reaching into how the transform was driven.
        /// <para>
        /// For a CMYK profile that puts K in red, and for an RGB one it answers the grey three times.
        /// </para>
        /// </summary>
        private sealed class EchoTransform(int components) : IIccTransform
        {
            public int NumberOfComponents { get; } = components;

            /// <summary>
            /// How many times the packed entry point was driven, which is the cost that scales with image
            /// size if the implementation gets it wrong.
            /// </summary>
            public int TransformCalls { get; private set; }

            public double[]? LastValues { get; private set; }

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values)
            {
                LastValues = values.ToArray();
                return (0.25, 0.5, 0.75);
            }

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb)
            {
                TransformCalls++;

                for (int p = 0; p < src.Length / NumberOfComponents; p++)
                {
                    int i = p * NumberOfComponents;

                    dstRgb[p * 3] = src[i + NumberOfComponents - 1];
                    dstRgb[p * 3 + 1] = src[i];
                    dstRgb[p * 3 + 2] = src[i + 1];
                }
            }
        }

        private sealed class RecordingProfile(IIccTransform transform) : IIccProfile
        {
            public int NumberOfComponents => transform.NumberOfComponents;

            public IReadOnlyList<double> ComponentRanges { get; } = new double[transform.NumberOfComponents * 2];

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? result)
            {
                result = transform;
                return true;
            }
        }

        private static IIccTransform? ImageTransform(ColorSpaceDetails colorSpace, IIccTransform inner)
            => OutputIntentColorManagement.GetDeviceImageTransform(colorSpace,
                RenderingIntent.RelativeColorimetric, new RecordingProfile(inner));

        [Fact]
        public void MatchingComponentCounts_HandBackTheProfileTransformItself()
        {
            // Nothing to adapt, so nothing is wrapped.
            var inner = new EchoTransform(4);

            Assert.Same(inner, ImageTransform(DeviceCmykColorSpaceDetails.Instance, inner));
        }

        [Fact]
        public void GrayImage_AgainstACmykProfile_YieldsATransformSizedForTheImage()
        {
            // The contract that lets a consumer stop caring: NumberOfComponents is the image's, so
            // src.Length == pixelCount * NumberOfComponents holds against the samples it actually has.
            var transform = ImageTransform(DeviceGrayColorSpaceDetails.Instance, new EchoTransform(4));

            Assert.NotNull(transform);
            Assert.Equal(1, transform.NumberOfComponents);
        }

        [Fact]
        public void GrayImage_AgainstACmykProfile_ExpandsEachSampleIntoTheBlackChannel()
        {
            // grey g -> (0, 0, 0, 1 - g), the byte-level counterpart of TryMapDeviceToProfileComponents.
            // EchoTransform puts the last component - K - in red.
            var transform = ImageTransform(DeviceGrayColorSpaceDetails.Instance, new EchoTransform(4))!;

            Span<byte> rgb = stackalloc byte[6];
            transform.Transform([0, 255], rgb);

            Assert.Equal(new byte[] { 255, 0, 0, 0, 0, 0 }, rgb.ToArray());
        }

        [Fact]
        public void GrayImage_AgainstAnRgbProfile_ReplicatesEachSample()
        {
            var transform = ImageTransform(DeviceGrayColorSpaceDetails.Instance, new EchoTransform(3))!;

            Span<byte> rgb = stackalloc byte[6];
            transform.Transform([10, 200], rgb);

            Assert.Equal(new byte[] { 10, 10, 10, 200, 200, 200 }, rgb.ToArray());
        }

        [Fact]
        public void GrayImage_DrivesTheProfileOnceWhateverTheImageSize()
        {
            // A one-component source has 256 possible values, so the profile is asked about those and no
            // more. Expanding every pixel into a 4x buffer and pushing all of it through instead is the
            // difference between 256 conversions and 65,536 for this image alone.
            var inner = new EchoTransform(4);
            var transform = ImageTransform(DeviceGrayColorSpaceDetails.Instance, inner)!;

            var samples = new byte[256 * 256];
            var rgb = new byte[samples.Length * 3];

            transform.Transform(samples, rgb);
            transform.Transform(samples, rgb);

            Assert.Equal(1, inner.TransformCalls);
        }

        [Fact]
        public void TheExpandingTransformMapsScalarConversionsToo()
        {
            // Both entry points of the wrapper expand the same way, so a consumer reaching for ToRgb on the
            // transform it was handed gets the same answer as the packed path.
            var inner = new EchoTransform(4);
            var transform = ImageTransform(DeviceGrayColorSpaceDetails.Instance, inner)!;

            transform.ToRgb([0.25]);

            Assert.Equal(new[] { 0.0, 0.0, 0.0, 0.75 }, inner.LastValues);
        }
    }
}
