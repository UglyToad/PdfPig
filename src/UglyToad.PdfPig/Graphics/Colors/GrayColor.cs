namespace UglyToad.PdfPig.Graphics.Colors
{
    /// <summary>
    /// A grayscale color with a single gray component.
    /// </summary>
    public sealed class GrayColor : IColor, IEquatable<GrayColor>
    {
        /// <summary>
        /// Gray Black value (0).
        /// </summary>
        public static GrayColor Black { get; } = new GrayColor(0);

        /// <summary>
        /// Gray White value (1).
        /// </summary>
        public static GrayColor White { get; } = new GrayColor(1);

        /// <inheritdoc/>
        public ColorSpace ColorSpace { get; } = ColorSpace.DeviceGray;

        /// <summary>
        /// The gray value between 0 and 1.
        /// </summary>
        public double Gray { get; }

        /// <summary>
        /// Create a new <see cref="GrayColor"/>.
        /// </summary>
        public GrayColor(double gray)
        {
            Gray = gray;
        }

        /// <inheritdoc/>
        public (double r, double g, double b) ToRGBValues()
        {
            return (Gray, Gray, Gray);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is GrayColor other && Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(GrayColor? other)
        {
            if (other is null)
            {
                return this is null;
            }

            return Gray == other.Gray;
        }

        /// <inheritdoc />
        public override int GetHashCode() => Gray.GetHashCode();

        /// <summary>
        /// Equals.
        /// </summary>
        public static bool operator ==(GrayColor color1, GrayColor color2) => EqualityComparer<GrayColor>.Default.Equals(color1, color2);

        /// <summary>
        /// Not Equals.
        /// </summary>
        public static bool operator !=(GrayColor color1, GrayColor color2) => !(color1 == color2);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Gray: {Gray}";
        }
    }
}
