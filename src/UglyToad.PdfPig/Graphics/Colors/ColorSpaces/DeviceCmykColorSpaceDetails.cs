namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using Core;

    /// <summary>
    /// Color values are defined by four components cyan, magenta, yellow and black.
    /// </summary>
    public sealed class DeviceCmykColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// The single instance of the <see cref="DeviceCmykColorSpaceDetails"/>.
        /// </summary>
        public static readonly DeviceCmykColorSpaceDetails Instance = new DeviceCmykColorSpaceDetails();

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 4;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private DeviceCmykColorSpaceDetails() : base(ColorSpace.DeviceCMYK)
        {
        }

        /// <inheritdoc/>
        internal override double[] Process(double[] values, RenderingIntent intent)
        {
            return values;
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
        {
            if (values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            double c = values[0];
            double m = values[1];
            double y = values[2];
            double k = values[3];
            if (c == 0 && m == 0 && y == 0 && k == 1)
            {
                return CMYKColor.Black;
            }

            if (c == 0 && m == 0 && y == 0 && k == 0)
            {
                return CMYKColor.White;
            }

            return new CMYKColor(c, m, y, k);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor(RenderingIntent intent)
        {
            return CMYKColor.Black;
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            double c = values[0];
            double m = values[1];
            double y = values[2];
            double k = values[3];
            double oneMinusK = 1.0 - k;
            r = (1.0 - c) * oneMinusK;
            g = (1.0 - m) * oneMinusK;
            b = (1.0 - y) * oneMinusK;
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
        {
            return decoded;
        }
    }
}
