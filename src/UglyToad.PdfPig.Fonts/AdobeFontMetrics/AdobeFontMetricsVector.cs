namespace UglyToad.PdfPig.Fonts.AdobeFontMetrics
{
    /// <summary>
    /// A vector in the Adobe Font Metrics.
    /// </summary>
    public struct AdobeFontMetricsVector
    {
        /// <summary>
        /// The x component of the vector.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// The y component of the vector.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Create a new <see cref="AdobeFontMetricsVector"/>.
        /// </summary>
        public AdobeFontMetricsVector(double x, double y)
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