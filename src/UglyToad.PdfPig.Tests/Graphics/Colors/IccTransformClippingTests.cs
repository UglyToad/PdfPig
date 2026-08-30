namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Core;
    using Xunit;

    /// <summary>
    /// What PdfPig does with the numbers an <see cref="IIccTransform"/> hands back. The interface documents
    /// <c>[0, 1]</c>, but it is third-party code, an absolute-colorimetric transform overshooting the gamut
    /// by a fraction is ordinary, and converting an out-of-range <see cref="double"/> to a
    /// <see cref="byte"/> is undefined in C# - so a fraction over 1.0 could become an arbitrary pixel.
    /// </summary>
    public class IccTransformClippingTests
    {
        private sealed class FixedTransform(int components, double r, double g, double b) : IIccTransform
        {
            public int NumberOfComponents { get; } = components;

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values) => (r, g, b);

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb) => dstRgb.Clear();
        }

        private sealed class FixedProfile(IIccTransform transform) : IIccProfile
        {
            public int NumberOfComponents => transform.NumberOfComponents;

            public IReadOnlyList<double> ComponentRanges { get; } = new double[transform.NumberOfComponents * 2];

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? result)
            {
                result = transform;
                return true;
            }
        }

        private static ICCBasedColorSpaceDetails Space(double r, double g, double b)
            => new(3, DeviceRgbColorSpaceDetails.Instance, null, null,
                new FixedProfile(new FixedTransform(3, r, g, b)));

        [Fact]
        public void ComponentsOutsideTheUnitRangeAreClipped()
        {
            // 1.5 and -0.25 are what a real transform produces at the edge of the gamut; neither is a
            // reason to paint an arbitrary colour.
            var (r, g, b) = Space(1.5, -0.25, 0.5).GetColor([0.0, 0.0, 0.0]).ToRGBValues();

            Assert.Equal(1.0, r);
            Assert.Equal(0.0, g);
            Assert.Equal(0.5, b);
        }

        [Fact]
        public void ClippingAppliesToTheUnboxedRgbPathToo()
        {
            Space(2.0, -1.0, 0.25).GetRgb([0.0, 0.0, 0.0], RenderingIntent.RelativeColorimetric,
                out double r, out double g, out double b);

            Assert.Equal(1.0, r);
            Assert.Equal(0.0, g);
            Assert.Equal(0.25, b);
        }

        [Fact]
        public void ANaNFallsBackToTheAlternateRatherThanPaintingBlack()
        {
            // There is no nearest valid value to substitute for a NaN, and the colour space already has a
            // well-defined answer for "the profile could not convert this colour". DeviceRGB is the
            // alternate here, so the operands come back untouched.
            var (r, g, b) = Space(0.5, double.NaN, 0.5).GetColor([0.1, 0.2, 0.3]).ToRGBValues();

            Assert.Equal(0.1, r, 6);
            Assert.Equal(0.2, g, 6);
            Assert.Equal(0.3, b, 6);
        }

        /// <summary>
        /// <see cref="ColorSpaceDetails.ConvertToByte"/> is protected, and it is the last line of defence
        /// for every colour space rather than only the ICC ones.
        /// </summary>
        private sealed class ByteConverter : ColorSpaceDetails
        {
            public ByteConverter() : base(ColorSpace.DeviceGray)
            {
            }

            public static byte Convert(double value) => ConvertToByte(value);

            public override int NumberOfColorComponents => 1;

            public override int BaseNumberOfColorComponents => 1;

            public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
                => throw new NotSupportedException();

            public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
                out double r, out double g, out double b) => throw new NotSupportedException();

            public override IColor? GetInitializeColor(RenderingIntent intent) => throw new NotSupportedException();

            internal override double[] Process(double[] values, RenderingIntent intent)
                => throw new NotSupportedException();

            internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
                => throw new NotSupportedException();
        }

        [Theory]
        [InlineData(0.0, 0)]
        [InlineData(1.0, 255)]
        [InlineData(0.5, 128)]      // 127.5 rounds away from zero, as it always did
        [InlineData(0.25, 64)]      // 63.75
        [InlineData(0.001, 0)]      // 0.255
        [InlineData(0.999, 255)]    // 254.745
        public void ConvertToByteMatchesTheRoundingItAlwaysHad(double value, byte expected)
        {
            Assert.Equal(expected, ByteConverter.Convert(value));
        }

        [Theory]
        [InlineData(-0.0001)]
        [InlineData(-1.0)]
        [InlineData(double.NaN)]
        [InlineData(double.NegativeInfinity)]
        public void ConvertToByteFloorsAnythingBelowTheRange(double value)
        {
            Assert.Equal(0, ByteConverter.Convert(value));
        }

        [Theory]
        [InlineData(1.0001)]
        [InlineData(2.0)]
        [InlineData(double.PositiveInfinity)]
        public void ConvertToByteCapsAnythingAboveTheRange(double value)
        {
            Assert.Equal(255, ByteConverter.Convert(value));
        }
    }
}
