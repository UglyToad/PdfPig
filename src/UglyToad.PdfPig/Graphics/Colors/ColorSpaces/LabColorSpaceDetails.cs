namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Collections.Generic;
    using Functions;

    /// <summary>
    /// CIE (Commission Internationale de l'Éclairage) colorspace.
    /// Specifies color related to human visual perception with the aim of producing consistent color on different output devices.
    /// CalRGB - A CIE ABC color space with a single transformation.
    /// A, B and C represent red, green and blue color values in the range 0.0 to 1.0.
    /// </summary>
    public sealed class LabColorSpaceDetails : ColorSpaceDetails
    {
        private readonly CIEBasedColorSpaceTransformer colorSpaceTransformer;

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 3;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

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
        /// An array of four numbers [a_min a_max b_min b_max] that shall specify the range of valid values for the a* and b* (B and C)
        /// components of the colour space — that is, a_min ≤ a* ≤ a_max and b_min ≤ b* ≤ b_max
        /// <para>Component values falling outside the specified range shall be adjusted to the nearest valid value without error indication.</para>
        /// Default value: [−100 100 −100 100].
        /// </summary>
        public IReadOnlyList<double> Matrix { get; }

        /// <summary>
        /// Create a new <see cref="LabColorSpaceDetails"/>.
        /// </summary>
        public LabColorSpaceDetails(double[] whitePoint, double[]? blackPoint, double[]? matrix)
            : base(ColorSpace.Lab)
        {
            WhitePoint = whitePoint ?? throw new ArgumentNullException(nameof(whitePoint));
            if (whitePoint.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(whitePoint), whitePoint, $"Must consist of exactly three numbers, but was passed {whitePoint.Length}.");
            }

            BlackPoint = blackPoint ?? [0.0, 0.0, 0.0];
            if (BlackPoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(blackPoint), blackPoint, $"Must consist of exactly three numbers, but was passed {blackPoint!.Length}.");
            }

            Matrix = matrix ?? [-100.0, 100.0, -100.0, 100.0];
            if (Matrix.Count != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(matrix), matrix, $"Must consist of exactly four numbers, but was passed {matrix!.Length}.");
            }

            colorSpaceTransformer = new CIEBasedColorSpaceTransformer((WhitePoint[0], WhitePoint[1], WhitePoint[2]), RGBWorkingSpace.sRGB);
        }

        /// <summary>
        /// Transforms the supplied ABC color to RGB (sRGB) using the properties of this <see cref="LabColorSpaceDetails"/>
        /// in the transformation process.
        /// A, B and C represent the L*, a*, and b* components of a CIE 1976 L*a*b* space. The range of the first (L*)
        /// component shall be 0 to 100; the ranges of the second and third (a* and b*) components shall be defined by
        /// the Range entry in the colour space dictionary
        /// </summary>
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

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Lab components do not decode to [0, 1]: L* decodes to [0, 100] and a*/b* decode to
        /// the ranges given by the colour space's Range entry (<see cref="Matrix"/>).
        /// </para>
        /// </summary>
        internal override void DecodeRawComponents(ReadOnlySpan<byte> raw, Span<double> destination)
        {
            for (int i = 0; i < raw.Length; i++)
            {
                double unit = raw[i] / 255.0;
                destination[i] = (i % 3) switch
                {
                    0 => unit * 100.0,                                            // L*: [0, 100]
                    1 => Matrix[0] + unit * (Matrix[1] - Matrix[0]),              // a*: Range
                    _ => Matrix[2] + unit * (Matrix[3] - Matrix[2]),              // b*: Range
                };
            }
        }

        private static double g(double x)
        {
            if (x > 6.0 / 29.0)
            {
                return x * x * x;
            }
            return 108.0 / 841.0 * (x - 4.0 / 29.0);
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
            double b = PdfFunction.ClipToRange(0, Matrix[0], Matrix[1]);
            double c = PdfFunction.ClipToRange(0, Matrix[2], Matrix[3]);
            Span<double> init = stackalloc double[3] { 0, b, c };
            GetRgb(init, out double rr, out double gg, out double bb);
            return new RGBColor(rr, gg, bb);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            // Component Ranges: L*: [0 100]; a* and b*: [-128 127]
            double bClip = PdfFunction.ClipToRange(values[1], Matrix[0], Matrix[1]);
            double cClip = PdfFunction.ClipToRange(values[2], Matrix[2], Matrix[3]);

            double M = (values[0] + 16.0) / 116.0;
            double L = M + (bClip / 500.0);
            double N = M - (cClip / 200.0);

            double X = WhitePoint[0] * LabColorSpaceDetails.g(L);
            double Y = WhitePoint[1] * LabColorSpaceDetails.g(M);
            double Z = WhitePoint[2] * LabColorSpaceDetails.g(N);

            (r, g, b) = colorSpaceTransformer.TransformToRGB((X, Y, Z));
        }
    }
}
