namespace UglyToad.PdfPig.Graphics.Colors
{
    /// <summary>
    /// A color with cyan, magenta, yellow and black (K) components.
    /// </summary>
    public sealed class CMYKColor : IColor, IEquatable<CMYKColor>
    {
        /// <summary>
        /// CMYK Black value (0, 0, 0, 1).
        /// </summary>
        public static IColor Black { get; } = new CMYKColor(0, 0, 0, 1);

        /// <summary>
        /// CMYK White value (all 0).
        /// </summary>
        public static IColor White { get; } = new CMYKColor(0, 0, 0, 0);

        /// <inheritdoc/>
        public ColorSpace ColorSpace { get; } = ColorSpace.DeviceCMYK;

        /// <summary>
        /// The cyan value.
        /// </summary>
        public double C { get; }

        /// <summary>
        /// The magenta value.
        /// </summary>
        public double M { get; }

        /// <summary>
        /// The yellow value.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// The black value.
        /// </summary>
        public double K { get; }

        /// <summary>
        /// Create a new <see cref="CMYKColor"/>.
        /// </summary>
        public CMYKColor(double c, double m, double y, double k)
        {
            C = c;
            M = m;
            Y = y;
            K = k;
        }

        /// <inheritdoc/>
        public (double r, double g, double b) ToRGBValues()
        {
            return ((1 - C) * (1 - K),
                (1 - M) * (1 - K),
                (1 - Y) * (1 - K));
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is CMYKColor other && Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(CMYKColor? other)
        {
            if (other is null)
            {
                return this is null;
            }

            return C == other.C &&
                   M == other.M &&
                   Y == other.Y &&
                   K == other.K;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(C, M, Y, K);
        }

        /// <summary>
        /// Equals.
        /// </summary>
        public static bool operator ==(CMYKColor color1, CMYKColor color2) => EqualityComparer<CMYKColor>.Default.Equals(color1, color2);

        /// <summary>
        /// Not Equals.
        /// </summary>
        public static bool operator !=(CMYKColor color1, CMYKColor color2) => !(color1 == color2);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CMYK: ({C}, {M}, {Y}, {K})";
        }
    }
}
