namespace UglyToad.PdfPig.Artwork
{
    using System;
    using System.Globalization;

    /// <summary>
    /// A DeviceRGB colour used by 3D artwork view settings, each component in the range 0 to 1.
    /// </summary>
    public readonly struct Artwork3DColor : IEquatable<Artwork3DColor>
    {
        /// <summary>
        /// White, the default background colour.
        /// </summary>
        public static Artwork3DColor White { get; } = new Artwork3DColor(1, 1, 1);

        /// <summary>
        /// Black, the default auxiliary colour.
        /// </summary>
        public static Artwork3DColor Black { get; } = new Artwork3DColor(0, 0, 0);

        /// <summary>The red component.</summary>
        public double R { get; }

        /// <summary>The green component.</summary>
        public double G { get; }

        /// <summary>The blue component.</summary>
        public double B { get; }

        /// <summary>
        /// Create a new <see cref="Artwork3DColor"/>.
        /// </summary>
        public Artwork3DColor(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <inheritdoc />
        public bool Equals(Artwork3DColor other) => R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Artwork3DColor other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (((R.GetHashCode() * 397) ^ G.GetHashCode()) * 397) ^ B.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", R, G, B);
    }
}
