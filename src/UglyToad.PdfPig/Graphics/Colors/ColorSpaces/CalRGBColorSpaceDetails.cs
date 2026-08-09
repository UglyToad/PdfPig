namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Collections.Generic;
    using Util;

    /// <summary>
    /// CIE (Commission Internationale de l'Éclairage) colorspace.
    /// Specifies color related to human visual perception with the aim of producing consistent color on different output devices.
    /// CalRGB - A CIE ABC color space with a single transformation.
    /// A, B and C represent red, green and blue color values in the range 0.0 to 1.0.
    /// </summary>
    public sealed class CalRGBColorSpaceDetails : ColorSpaceDetails
    {
        /// <inheritdoc/>
        public override int NumberOfColorComponents => 3;

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
        /// An array of three numbers [GR  GG  GB] specifying the gamma for the red, green and blue (A, B, C) components
        /// of the color space. Default value: [1.0  1.0  1.0].
        /// </summary>
        public IReadOnlyList<double> Gamma { get; }

        /// <summary>
        /// An array of nine numbers [XA  YA  ZA  XB  YB  ZB  XC  YC  ZC] specifying the linear interpretation of the
        /// decoded A, B, C components of the color space with respect to the final XYZ representation. Default value:
        /// [1  0  0  0  1  0  0  0  1].
        /// </summary>
        public IReadOnlyList<double> Matrix { get; }

        /// <summary>
        /// Create a new <see cref="CalRGBColorSpaceDetails"/>.
        /// </summary>
        public CalRGBColorSpaceDetails(double[] whitePoint, double[]? blackPoint, double[]? gamma, double[]? matrix)
            : base(ColorSpace.CalRGB)
        {
            WhitePoint = whitePoint ?? throw new ArgumentNullException(nameof(whitePoint));
            if (WhitePoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(whitePoint), whitePoint, $"Must consist of exactly three numbers, but was passed {whitePoint.Length}.");
            }

            BlackPoint = blackPoint ?? [0.0, 0, 0];
            if (BlackPoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(blackPoint), blackPoint, $"Must consist of exactly three numbers, but was passed {blackPoint!.Length}.");
            }

            Gamma = gamma ?? [1.0, 1, 1];
            if (Gamma.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(gamma), gamma, $"Must consist of exactly three numbers, but was passed {gamma!.Length}.");
            }

            Matrix = matrix ?? [1.0, 0, 0, 0, 1, 0, 0, 0, 1];
            if (Matrix.Count != 9)
            {
                throw new ArgumentOutOfRangeException(nameof(matrix), matrix, $"Must consist of exactly nine numbers, but was passed {matrix!.Length}.");
            }

            colorSpaceTransformer =
                new CIEBasedColorSpaceTransformer((WhitePoint[0], WhitePoint[1], WhitePoint[2]), RGBWorkingSpace.sRGB)
                {
                    DecoderABC = color => (
                    Math.Pow(color.A, Gamma[0]),
                    Math.Pow(color.B, Gamma[1]),
                    Math.Pow(color.C, Gamma[2])),

                    MatrixABC = new Matrix3x3(
                    Matrix[0], Matrix[3], Matrix[6],
                    Matrix[1], Matrix[4], Matrix[7],
                    Matrix[2], Matrix[5], Matrix[8])
                };
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
        {
            var transformed = new byte[decoded.Length];
            int index = 0;
            Span<double> input = stackalloc double[3];

            for (var i = 0; i < decoded.Length; i += 3)
            {
                input[0] = decoded[i] / 255.0;
                input[1] = decoded[i + 1] / 255.0;
                input[2] = decoded[i + 2] / 255.0;
                GetRgb(input, out double r, out double g, out double b);
                transformed[index++] = ConvertToByte(r);
                transformed[index++] = ConvertToByte(g);
                transformed[index++] = ConvertToByte(b);
            }

            return transformed;
        }

        /// <inheritdoc/>
        internal override double[] Process(double[] values, RenderingIntent intent)
        {
            GetRgb(values, out double r, out double g, out double b);
            return [r, g, b];
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
            Span<double> zero = stackalloc double[3];
            GetRgb(zero, out double r, out double g, out double b);
            return new RGBColor(r, g, b);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            (r, g, b) = colorSpaceTransformer.TransformToRGB((values[0], values[1], values[2]));
        }
    }
}
