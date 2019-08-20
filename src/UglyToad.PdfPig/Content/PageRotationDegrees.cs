namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Diagnostics.Contracts;
    using Core;

    /// <summary>
    /// Represents the rotation of a page in a PDF document defined by the page dictionary in degrees clockwise.
    /// </summary>
    public struct PageRotationDegrees : IEquatable<PageRotationDegrees>
    {
        private static readonly TransformationMatrix Rotate90 = TransformationMatrix.FromValues(0m, -1, 1, 0);
        private static readonly TransformationMatrix Rotate180 = TransformationMatrix.FromValues(-1m, 0, 0, -1);
        private static readonly TransformationMatrix Rotate270 = TransformationMatrix.FromValues(0m, 1, -1, 0);

        /// <summary>
        /// The rotation of the page in degrees clockwise.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Get the rotation expressed in radians (anti-clockwise).
        /// </summary>
        public decimal Radians
        {
            get
            {
                switch (Value)
                {
                    case 0:
                        return 0;
                    case 90:
                        return -(decimal)(0.5 * Math.PI);
                    case 180:
                        return -(decimal) Math.PI;
                    case 270:
                        return -(decimal) (1.5 * Math.PI);
                    default:
                        throw new InvalidOperationException($"Invalid value for rotation: {Value}.");
                }
            }
        }

        /// <summary>
        /// Create a <see cref="PageRotationDegrees"/>.
        /// </summary>
        /// <param name="rotation">Rotation in degrees clockwise.</param>
        public PageRotationDegrees(int rotation)
        {
            if (rotation != 0 && rotation != 90 && rotation != 180 && rotation != 270)
            {
                throw new ArgumentOutOfRangeException(nameof(rotation), $"Rotation must be 0, 90, 180 or 270. Got: {rotation}.");
            }

            Value = rotation;
        }
        
        [Pure]
        internal TransformationMatrix Rotate(TransformationMatrix matrix)
        {
            switch (Value)
            {
                case 0:
                    return matrix;
                case 90:
                    return Rotate90.Multiply(matrix);
                case 180:
                    return Rotate180.Multiply(matrix);
                case 270:
                    return Rotate270.Multiply(matrix);
                default:
                    throw new InvalidOperationException($"Invalid value for rotation: {Value}.");
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.ToString();
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
