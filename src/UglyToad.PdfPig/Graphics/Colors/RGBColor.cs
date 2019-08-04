namespace UglyToad.PdfPig.Graphics.Colors
{
    /// <summary>
    /// A color with red, green and blue components.
    /// </summary>
    internal class RGBColor : IColor
    {
        public static RGBColor Black = new RGBColor(0, 0, 0);
        public static RGBColor White = new RGBColor(1, 1, 1);

        /// <inheritdoc/>
        public ColorSpace ColorSpace { get; } = ColorSpace.DeviceRGB;

        /// <summary>
        /// The red value.
        /// </summary>
        public decimal R { get; }

        /// <summary>
        /// The green value.
        /// </summary>
        public decimal G { get; }

        /// <summary>
        /// The blue value.
        /// </summary>
        public decimal B { get; }

        /// <summary>
        /// Create a new <see cref="RGBColor"/>.
        /// </summary>
        public RGBColor(decimal r, decimal g, decimal b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <inheritdoc/>
        public (decimal r, decimal g, decimal b) ToRGBValues()
        {
            return (R, G, B);
        }
    }
}