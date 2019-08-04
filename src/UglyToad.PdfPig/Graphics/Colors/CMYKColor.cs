namespace UglyToad.PdfPig.Graphics.Colors
{
    /// <summary>
    /// A color with cyan, magenta, yellow and black (K) components.
    /// </summary>
    internal class CMYKColor : IColor
    {
        public static IColor Black { get; } = new CMYKColor(0, 0, 0, 1);
        public static IColor White { get; } = new CMYKColor(0, 0, 0, 0);

        /// <inheritdoc/>
        public ColorSpace ColorSpace { get; } = ColorSpace.DeviceCMYK;

        /// <summary>
        /// The cyan value.
        /// </summary>
        public decimal C { get; }

        /// <summary>
        /// The magenta value.
        /// </summary>
        public decimal M { get; }

        /// <summary>
        /// The yellow value.
        /// </summary>
        public decimal Y { get; }

        /// <summary>
        /// The black value.
        /// </summary>
        public decimal K { get; }

        /// <summary>
        /// Create a new <see cref="CMYKColor"/>.
        /// </summary>
        public CMYKColor(decimal c, decimal m, decimal y, decimal k)
        {
            C = c;
            M = m;
            Y = y;
            K = k;
        }

        /// <inheritdoc/>
        public (decimal r, decimal g, decimal b) ToRGBValues()
        {
            return ((255 * (1 - C) * (1 - K)) / 255m,
                (255 * (1 - M) * (1 - K)) / 255m,
                (255 * (1 - Y) * (1 - K)) / 255m);
        }
    }
}