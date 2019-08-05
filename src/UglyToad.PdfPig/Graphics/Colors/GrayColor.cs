namespace UglyToad.PdfPig.Graphics.Colors
{
    /// <summary>
    /// A grayscale color with a single gray component.
    /// </summary>
    internal class GrayColor : IColor
    {
        public static GrayColor Black { get; } = new GrayColor(0);
        public static GrayColor White { get; } = new GrayColor(1);

        /// <inheritdoc/>
        public ColorSpace ColorSpace { get; } = ColorSpace.DeviceGray;

        /// <summary>
        /// The gray value between 0 and 1.
        /// </summary>
        public decimal Gray { get; }

        /// <summary>
        /// Create a new <see cref="GrayColor"/>.
        /// </summary>
        public GrayColor(decimal gray)
        {
            Gray = gray;
        }

        /// <inheritdoc/>
        public (decimal r, decimal g, decimal b) ToRGBValues()
        {
            return (Gray, Gray, Gray);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Gray: {Gray}";
        }
    }
}