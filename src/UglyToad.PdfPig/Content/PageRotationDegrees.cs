namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Represents the rotation of a page in a PDF document defined by the page dictionary in degrees clockwise.
    /// </summary>
    public readonly struct PageRotationDegrees : IEquatable<PageRotationDegrees>
    {
        /// <summary>
        /// The rotation of the page in degrees clockwise.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Whether the rotation flips the x and y axes.
        /// </summary>
        public bool SwapsAxis => (Value == 90) || (Value == 270);

        /// <summary>
        /// Get the rotation expressed in radians (anti-clockwise).
        /// </summary>
        public double Radians
        {
            get
            {
                return Value switch {
                    0   => 0,
                    90  => -0.5 * Math.PI,
                    180 => -Math.PI,
                    270 => -1.5 * Math.PI,
                    _   => throw new InvalidOperationException($"Invalid value for rotation: {Value}.")
                };
            }
        }

        /// <summary>
        /// Create a <see cref="PageRotationDegrees"/>.
        /// </summary>
        /// <param name="rotation">Rotation in degrees clockwise, must be a multiple of 90.</param>
        public PageRotationDegrees(int rotation)
        {
            if (rotation < 0)
            {
                rotation = 360 + rotation;
            }

            while (rotation >= 360)
            {
                rotation -= 360;
            }

            if (rotation != 0 && rotation != 90 && rotation != 180 && rotation != 270)
            {
                throw new ArgumentOutOfRangeException(nameof(rotation), $"Rotation must be 0, 90, 180 or 270. Got: {rotation}.");
            }

            Value = rotation;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is PageRotationDegrees degrees && Equals(degrees);
        }

        /// <inheritdoc />
        public bool Equals(PageRotationDegrees other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// Equal.
        /// </summary>
        public static bool operator ==(PageRotationDegrees degrees1, PageRotationDegrees degrees2)
        {
            return degrees1.Equals(degrees2);
        }

        /// <summary>
        /// Not equal.
        /// </summary>
        public static bool operator !=(PageRotationDegrees degrees1, PageRotationDegrees degrees2) => !(degrees1 == degrees2);
    }
}
