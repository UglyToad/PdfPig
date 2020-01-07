namespace UglyToad.PdfPig.Fonts.AdobeFontMetrics
{
    /// <summary>
    /// The x and y components of the width vector of the font's characters.
    /// Presence implies that IsFixedPitch is true.
    /// </summary>
    public class AdobeFontMetricsCharacterSize
    {
        /// <summary>
        /// The horizontal width.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// The vertical width.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Create a new <see cref="AdobeFontMetricsCharacterSize"/>.
        /// </summary>
        public AdobeFontMetricsCharacterSize(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{X}, {Y}";
        }
    }
}