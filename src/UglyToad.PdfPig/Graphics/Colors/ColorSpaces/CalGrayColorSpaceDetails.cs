namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Collections.Generic;
    using Util;

    /// <summary>
    /// CIE (Commission Internationale de l'Éclairage) colorspace.
    /// Specifies color related to human visual perception with the aim of producing consistent color on different output devices.
    /// CalGray - A CIE A color space with a single transformation.
    /// A represents the gray component of a calibrated gray space. The component must be in the range 0.0 to 1.0.
    /// </summary>
    public sealed class CalGrayColorSpaceDetails : ColorSpaceDetails
    {
        /// <inheritdoc/>
        public override int NumberOfColorComponents => 1;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private readonly CIEBasedColorSpaceTransformer colorSpaceTransformer;

        /// <summary>
        /// An array of three numbers [XW  YW  ZW] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse white point. The numbers XW and ZW shall be positive, and YW shall be equal to 1.0.
        /// </summary>
        public IReadOnlyList<double> WhitePoint { get; }

        /// <summary>
        /// An array of three numbers [XB  YB  ZB] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse black point. All three numbers must be non-negative. Default value: [0.0  0.0  0.0].
        /// </summary>
        public IReadOnlyList<double> BlackPoint { get; }

        /// <summary>
        /// A number defining the gamma for the gray (A) component. Gamma must be positive and is generally
        /// greater than or equal to 1. Default value: 1.
        /// </summary>
        public double Gamma { get; }

        /// <summary>
        /// Create a new <see cref="CalGrayColorSpaceDetails"/>.
        /// </summary>
        public CalGrayColorSpaceDetails(double[] whitePoint, double[]? blackPoint, double? gamma)
            : base(ColorSpace.CalGray)
        {
            WhitePoint = whitePoint ?? throw new ArgumentNullException(nameof(whitePoint));
            if (WhitePoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(whitePoint), whitePoint, $"Must consist of exactly three numbers, but was passed {whitePoint.Length}.");
            }

            BlackPoint = blackPoint ?? [0.0, 0, 0];
            if (BlackPoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(blackPoint), blackPoint, $"Must consist of exactly three numbers, but was passed {blackPoint?.Length ?? 0}.");
            }

            Gamma = gamma ?? 1.0;

            colorSpaceTransformer =
                new CIEBasedColorSpaceTransformer((WhitePoint[0], WhitePoint[1], WhitePoint[2]), RGBWorkingSpace.sRGB)
                {
                    DecoderABC = color => (
                    Math.Pow(color.A, Gamma),
                    Math.Pow(color.B, Gamma),
                    Math.Pow(color.C, Gamma)),

                    MatrixABC = new Matrix3x3(
                    WhitePoint[0], 0, 0,
                    0, WhitePoint[1], 0,
                    0, 0, WhitePoint[2])
                };
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
        {
            var transformed = new byte[decoded.Length];
            Span<double> input = stackalloc double[1];

            for (var i = 0; i < decoded.Length; i++)
            {
                input[0] = decoded[i] / 255.0;
                GetRgb(input, out double r, out _, out _);
                // We only need one component here
                transformed[i] = ConvertToByte(r);
            }

            return transformed;
        }

        /// <inheritdoc/>
        internal override double[] Process(double[] values, RenderingIntent intent)
        {
            GetRgb(values, out double r, out _, out _);
            return [r];
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
        {
            if (values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            GetRgb(values, out double r, out double g, out double b);
            return new RGBColor(r, g, b);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to any CIE-based colour space shall
            // initialize all components of the corresponding current colour to 0.0 (unless the range of valid
            // values for a given component does not include 0.0, in which case the nearest valid value shall
            // be substituted.)
            return GetColor([0]);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            double a = values[0];
            (r, g, b) = colorSpaceTransformer.TransformToRGB((a, a, a));
        }
    }
}
