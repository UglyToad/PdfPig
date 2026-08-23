namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System;

    /// <summary>
    /// Reading a colour back out of an <see cref="IIccTransform"/>, which is third-party code that PdfPig
    /// takes at its word about everything except the numbers themselves.
    /// </summary>
    internal static class IccTransformExtensions
    {
        /// <summary>
        /// Convert one colour and clip each component into <c>[0, 1]</c>, reporting <see langword="false"/>
        /// when the transform answered with a <see cref="double.NaN"/> and so cannot be believed for this
        /// colour at all.
        /// <para>
        /// <see cref="IIccTransform.ToRgb"/> is documented as returning <c>[0, 1]</c>, but an
        /// absolute-colorimetric transform overshooting the gamut by a fraction is ordinary, and the values
        /// flow straight into an <see cref="RGBColor"/> and on into a caller's byte conversion. Clipping
        /// here is what keeps "slightly out of gamut" from becoming "wrapped to an arbitrary byte".
        /// </para>
        /// <para>
        /// A NaN is different in kind: there is no nearest valid value to substitute, and every caller of
        /// this already has a well-defined answer for "the profile could not convert this colour" - the
        /// alternate colour space, or the built-in device conversion. Reporting failure hands the colour to
        /// that rather than painting it black.
        /// </para>
        /// </summary>
        public static bool TryToRgbClipped(this IIccTransform transform, ReadOnlySpan<double> values,
            out double r, out double g, out double b)
        {
            (r, g, b) = transform.ToRgb(values);

            if (double.IsNaN(r) || double.IsNaN(g) || double.IsNaN(b))
            {
                r = g = b = 0.0;
                return false;
            }

            r = Clip(r);
            g = Clip(g);
            b = Clip(b);
            return true;
        }

        private static double Clip(double value) => value < 0.0 ? 0.0 : value > 1.0 ? 1.0 : value;
    }
}
