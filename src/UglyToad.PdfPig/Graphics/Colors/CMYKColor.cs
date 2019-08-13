using System;
using System.Collections.Generic;

namespace UglyToad.PdfPig.Graphics.Colors
{
    /// <summary>
    /// A color with cyan, magenta, yellow and black (K) components.
    /// </summary>
    public class CMYKColor : IColor, IEquatable<CMYKColor>
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
            return ((1 - C) * (1 - K),
                (1 - M) * (1 - K),
                (1 - Y) * (1 - K));
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as CMYKColor);
        }

        /// <inheritdoc />
        public bool Equals(CMYKColor other)
        {
            return other != null &&
                   C == other.C &&
                   M == other.M &&
                   Y == other.Y &&
                   K == other.K;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = -492570696;
            hashCode = hashCode * -1521134295 + C.GetHashCode();
            hashCode = hashCode * -1521134295 + M.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + K.GetHashCode();
            return hashCode;
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