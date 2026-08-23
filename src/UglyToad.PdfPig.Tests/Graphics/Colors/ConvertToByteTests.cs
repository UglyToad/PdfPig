namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using PdfPig.Graphics.Colors;
    using System;
    using UglyToad.PdfPig.Tests.Integration;
    using Xunit;

    /// <summary>
    /// <see cref="ColorSpaceDetails.ConvertToByte"/> is the last line of defence for every colour space:
    /// a component that arrives outside <c>[0, 1]</c> must be clipped rather than cast, because casting a
    /// <see cref="double"/> outside <see cref="byte"/>'s range is not valid in C#.
    /// </summary>
    public class ConvertToByteTests
    {
        /// <summary>
        /// <see cref="ColorSpaceDetails.ConvertToByte"/> is protected, so reaching it means deriving.
        /// </summary>
        private sealed class ByteConverter : ColorSpaceDetails
        {
            public ByteConverter() : base(ColorSpace.DeviceGray)
            {
            }

            public static byte Convert(double value) => ConvertToByte(value);

            public override int NumberOfColorComponents => 1;

            public override int BaseNumberOfColorComponents => 1;

            public override IColor GetColor(ReadOnlySpan<double> values) => throw new NotSupportedException();

            public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
                => throw new NotSupportedException();

            public override IColor? GetInitializeColor() => throw new NotSupportedException();

            internal override double[] Process(params double[] values) => throw new NotSupportedException();

            internal override Span<byte> Transform(Span<byte> decoded) => throw new NotSupportedException();
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

        [Fact]
        public void SeparationLabColorSpace()
        {
            // Test with TIKA_1552_0.pdf
            // https://icolorpalette.com/color/pantone-289-c
            // Pantone 289 C Color | #0C2340
            // Rgb : rgb(12,35,64)
            // CIE L*a*b* : 13.53, 2.89, -21.08

            var path = IntegrationHelpers.GetDocumentPath("TIKA-1552-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page1 = document.GetPage(1);

                var background = page1.Paths[0];
                Assert.True(background.IsFilled);

                var (r, g, b) = background.FillColor.ToRGBValues();

                // Colors picked from Acrobat reader: rgb(11, 34, 64)
                Assert.Equal(10, ByteConverter.Convert(r)); // Should be 11, but close enough
                Assert.Equal(34, ByteConverter.Convert(g));
                Assert.Equal(64, ByteConverter.Convert(b));
            }
        }

    }
}
