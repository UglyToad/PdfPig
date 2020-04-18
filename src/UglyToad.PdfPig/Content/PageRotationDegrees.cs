namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Diagnostics.Contracts;
    using Core;
    using Geometry;

    /// <summary>
    /// Represents the rotation of a page in a PDF document defined by the page dictionary in degrees clockwise.
    /// </summary>
    public struct PageRotationDegrees : IEquatable<PageRotationDegrees>
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
        
        [Pure]
        internal PdfRectangle Rotate(PdfRectangle rectangle, PdfVector pageSize)
        {
            // TODO: this is a bit of a hack because I don't understand matrices
            /* There should be a single Affine Transform we can apply to any point resulting
             * from a content stream operation which will rotate the point and translate it back to
             * a point where the origin is in the page's lower left corner.
             *
             * For example this matrix represents a (clockwise) rotation and translation:
             * [  cos  sin  tx ]
             * [ -sin  cos  ty ]
             * [    0    0   1 ]
             *
             * The values of tx and ty are those required to move the origin back to the expected origin (lower-left).
             * The corresponding values should be:
             * Rotation:  0   90  180  270
             *       tx:  0    0    w    w
             *       ty:  0    h    h    0
             *
             * Where w and h are the page width and height after rotation.
            */        
            double cos, sin;
            double dx = 0, dy = 0;
            switch (Value)
            {
                case 0:
                    return rectangle;
                case 90:
                    cos = 0;
                    sin = 1;
                    dy = pageSize.Y;
                    break;
                case 180:
                    cos = -1;
                    sin = 0;
                    dx = pageSize.X;
                    dy = pageSize.Y;
                    break;
                case 270:
                    cos = 0;
                    sin = -1;
                    dx = pageSize.X;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value for rotation: {Value}.");
            }

            PdfPoint Multiply(PdfPoint pt)
            {
                return new PdfPoint((pt.X * cos) + (pt.Y * sin) + dx,
                    (pt.X * -sin) + (pt.Y * cos) + dy);
            }

            return new PdfRectangle(Multiply(rectangle.TopLeft), Multiply(rectangle.TopRight),
                Multiply(rectangle.BottomLeft), Multiply(rectangle.BottomRight));
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
