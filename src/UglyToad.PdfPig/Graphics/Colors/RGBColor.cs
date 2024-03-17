namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A color with red, green and blue components.
    /// </summary>
    public sealed class RGBColor : IColor, IEquatable<RGBColor>
    {
        /// <summary>
        /// RGB Black value (all 0).
        /// </summary>
        public static RGBColor Black = new RGBColor(0, 0, 0);

        /// <summary>
        /// RGB White value (all 1).
        /// </summary>
        public static RGBColor White = new RGBColor(1, 1, 1);

        /// <inheritdoc/>
        public ColorSpace ColorSpace { get; } = ColorSpace.DeviceRGB;

        /// <summary>
        /// The red value between 0 and 1.
        /// </summary>
        public double R { get; }

        /// <summary>
        /// The green value between 0 and 1.
        /// </summary>
        public double G { get; }

        /// <summary>
        /// The blue value between 0 and 1.
        /// </summary>
        public double B { get; }

        /// <summary>
        /// Create a new <see cref="RGBColor"/>.
        /// </summary>
        /// <param name="r">The red value between 0 and 1.</param>
        /// <param name="g">The green value between 0 and 1.</param>
        /// <param name="b">The blue value between 0 and 1.</param>
        public RGBColor(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <inheritdoc/>
        public (double r, double g, double b) ToRGBValues() => (R, G, B);

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is RGBColor other && Equals(other);
        }

        /// <inheritdoc />
        /// <summary>
        /// Whether 2 RGB colors are equal across all channels.
        /// </summary>
        public bool Equals(RGBColor? other)
        {
            if (other is null)
            {
                return this is null;
            }

            return R == other.R
                && G == other.G
                && B == other.B;
        }

        /// <inheritdoc />
        public override int GetHashCode() => (R, G, B).GetHashCode();

        /// <summary>
        /// Equals.
        /// </summary>
        public static bool operator ==(RGBColor color1, RGBColor color2) =>
            EqualityComparer<RGBColor>.Default.Equals(color1, color2);

        /// <summary>
        /// Not Equals.
        /// </summary>
        public static bool operator !=(RGBColor color1, RGBColor color2) => !(color1 == color2);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"RGB: ({R}, {G}, {B})";
        }
    }
}
