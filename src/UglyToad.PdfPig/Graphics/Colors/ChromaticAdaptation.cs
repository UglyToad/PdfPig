namespace UglyToad.PdfPig.Graphics.Colors
{
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// Encapsulates the algorithm for chromatic adaptation described here:
    /// http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html
    /// </summary>
    internal class ChromaticAdaptation
    {
        public enum Method { XYZScaling, Bradford, VonKries };

        private readonly Matrix3x3 adaptationMatrix;

        public ChromaticAdaptation(
            (double Xws, double Yws, double Zws) sourceReferenceWhite,
            (double Xwd, double Ywd, double Zwd) destinationReferenceWhite,
            Method method = Method.Bradford)
        {
            var coneReponseDomain = GetConeResponseDomain(method)!;
            var inverseConeResponseDomain = coneReponseDomain.Inverse();
            var (ρS, γS, βS) = coneReponseDomain.Multiply(sourceReferenceWhite);

            var (ρD, γD, βD) = coneReponseDomain.Multiply(destinationReferenceWhite);

            var scale = new Matrix3x3(
                ρD / ρS, 0, 0,
                0, γD / γS, 0,
                0, 0, βD / βS);

            adaptationMatrix = inverseConeResponseDomain.Multiply(scale).Multiply(coneReponseDomain);
        }

        public (double X, double Y, double Z) Transform((double X, double Y, double Z) sourceColor)
        {
            return adaptationMatrix.Multiply(sourceColor);
        }

        private static Matrix3x3 GetConeResponseDomain(Method method)
        {
            switch (method)
            {
                case Method.XYZScaling:
                    return new Matrix3x3(
                        1.0000000, 0.0000000, 0.0000000,
                        0.0000000, 1.0000000, 0.0000000,
                        0.0000000, 0.0000000, 1.0000000);

                case Method.Bradford:
                    return new Matrix3x3(
                        0.8951000, 0.2664000, -0.1614000,
                       -0.7502000, 1.7135000, 0.0367000,
                        0.0389000, -0.0685000, 1.0296000);

                case Method.VonKries:
                    return new Matrix3x3(
                        0.4002400, 0.7076000, -0.0808100,
                       -0.2263000, 1.1653200, 0.0457000,
                        0.0000000, 0.0000000, 0.9182200);

                default:
                    return GetConeResponseDomain(Method.Bradford);
            }
        }
    }
}
