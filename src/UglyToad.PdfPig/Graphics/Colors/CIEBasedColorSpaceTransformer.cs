namespace UglyToad.PdfPig.Graphics.Colors
{
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// Transformer for CIEBased color spaces.
    /// <para>
    /// In addition to the PDF spec itself, the transformation implementation is based on the descriptions in:
    /// https://en.wikipedia.org/wiki/SRGB#The_forward_transformation_(CIE_XYZ_to_sRGB) and
    /// http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_RGB.html
    /// </para>
    /// </summary>
    internal class CIEBasedColorSpaceTransformer
    {
        private readonly RGBWorkingSpace destinationWorkingSpace;
        private readonly Matrix3x3 transformationMatrix;
        private readonly ChromaticAdaptation chromaticAdaptation;

        // These properties control how the color is translated from ABC to XYZ:
        public Func<(double A, double B, double C), (double A, double B, double C)> DecoderABC { get; set; } = color => color;
        public Func<(double L, double M, double N), (double L, double M, double N)> DecoderLMN { get; set; } = color => color;
        public Matrix3x3 MatrixABC { get; set; } = Matrix3x3.Identity;
        public Matrix3x3 MatrixLMN { get; set; } = Matrix3x3.Identity;

        public CIEBasedColorSpaceTransformer((double X, double Y, double Z) sourceReferenceWhite, RGBWorkingSpace destinationWorkingSpace)
        {
            this.destinationWorkingSpace = destinationWorkingSpace;

            // Create an adapter capable of adapting from one reference white to another
            chromaticAdaptation = new ChromaticAdaptation(sourceReferenceWhite, destinationWorkingSpace.ReferenceWhite);

            // Construct the transformation matrix capable of transforming from XYZ of the source color space
            // to RGB of the destination color space
            var xr = destinationWorkingSpace.RedPrimary.x;
            var yr = destinationWorkingSpace.RedPrimary.y;

            var xg = destinationWorkingSpace.GreenPrimary.x;
            var yg = destinationWorkingSpace.GreenPrimary.y;

            var xb = destinationWorkingSpace.BluePrimary.x;
            var yb = destinationWorkingSpace.BluePrimary.y;

            var Xr = xr / yr;
            var Yr = 1;
            var Zr = (1 - xr - yr) / yr;

            var Xg = xg / yg;
            var Yg = 1;
            var Zg = (1 - xg - yg) / yg;

            var Xb = xb / yb;
            var Yb = 1;
            var Zb = (1 - xb - yb) / yb;

            var mXYZ = new Matrix3x3(
                Xr, Xg, Xb,
                Yr, Yg, Yb,
                Zr, Zg, Zb).Inverse()!;

            var S = mXYZ.Multiply(destinationWorkingSpace.ReferenceWhite);

            var Sr = S.Item1;
            var Sg = S.Item2;
            var Sb = S.Item3;

            var M = new Matrix3x3(
                Sr * Xr, Sg * Xg, Sb * Xb,
                Sr * Yr, Sg * Yg, Sb * Yb,
                Sr * Zr, Sg * Zg, Sb * Zb);

            transformationMatrix = M.Inverse()!;
        }

        /// <summary>
        /// Transforms the supplied ABC color to the RGB color of the <see cref="RGBWorkingSpace"/>
        /// that was supplied to this <see cref="CIEBasedColorSpaceTransformer"/> as the destination
        /// workspace.
        /// A, B and C represent red, green and blue calibrated color values in the range 0 to 1.
        /// </summary>
        public (double R, double G, double B) TransformToRGB((double A, double B, double C) color)
        {
            var xyz = TransformToXYZ(color);

            var adaptedColor = chromaticAdaptation.Transform(xyz);
            var rgb = transformationMatrix.Multiply(adaptedColor);

            var gammaCorrectedR = destinationWorkingSpace.GammaCorrection(rgb.Item1);
            var gammaCorrectedG = destinationWorkingSpace.GammaCorrection(rgb.Item2);
            var gammaCorrectedB = destinationWorkingSpace.GammaCorrection(rgb.Item3);

            return (Clamp(gammaCorrectedR), Clamp(gammaCorrectedG), Clamp(gammaCorrectedB));
        }

        private (double X, double Y, double Z) TransformToXYZ((double A, double B, double C) color)
        {
            var decodedABC = DecoderABC(color);
            var lmn = MatrixABC.Multiply(decodedABC);

            var decodedLMN = DecoderLMN(lmn);
            var xyz = MatrixLMN.Multiply(decodedLMN);

            return xyz;
        }

        private static double Clamp(double value)
        {
            // Force value into range [0..1]
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }
    }
}
