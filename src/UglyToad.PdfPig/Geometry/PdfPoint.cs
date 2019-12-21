namespace UglyToad.PdfPig.Geometry
{
    using System.Diagnostics;

    /// <summary>
    /// A point in a PDF file. 
    /// </summary>
    /// <remarks>
    /// PDF coordinates are defined with the origin at the lower left (0, 0).
    /// The Y-axis extends vertically upwards and the X-axis horizontally to the right.
    /// Unless otherwise specified on a per-page basis, units in PDF space are equivalent to a typographic point (1/72 inch).
    /// </remarks>
    public struct PdfPoint
    {
        /// <summary>
        /// The origin of the coordinates system.
        /// </summary>
        public static PdfPoint Origin { get; } = new PdfPoint(0m, 0m);

        /// <summary>
        /// The X coordinate for this point. (Horizontal axis).
        /// </summary>
        public double X { get; }

        /// <summary>
        /// The Y coordinate of this point. (Vertical axis).
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Create a new <see cref="PdfPoint"/> at this position.
        /// </summary>
        [DebuggerStepThrough]
        public PdfPoint(decimal x, decimal y)
        {
            X = (double)x;
            Y = (double)y;
        }

        /// <summary>
        /// Create a new <see cref="PdfPoint"/> at this position.
        /// </summary>
        [DebuggerStepThrough]
        public PdfPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Create a new <see cref="PdfPoint"/> at this position.
        /// </summary>
        [DebuggerStepThrough]
        public PdfPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Creates a new <see cref="PdfPoint"/> which is the current point moved in the x direction relative to its current position by a value.
        /// </summary>
        /// <param name="dx">The distance to move the point in the x direction relative to its current location.</param>
        /// <returns>A new point shifted on the x axis by the given delta value.</returns>
        public PdfPoint MoveX(double dx)
        {
            return new PdfPoint(X + dx, Y);
        }
        
        /// <summary>
        /// Creates a new <see cref="PdfPoint"/> which is the current point moved in the y direction relative to its current position by a value.
        /// </summary>
        /// <param name="dy">The distance to move the point in the y direction relative to its current location.</param>
        /// <returns>A new point shifted on the y axis by the given delta value.</returns>
        public PdfPoint MoveY(double dy)
        {
            return new PdfPoint(X, Y + dy);
        }

        /// <summary>
        /// Creates a new <see cref="PdfPoint"/> which is the current point moved in the x and y directions relative to its current position by a value.
        /// </summary>
        /// <param name="dx">The distance to move the point in the x direction relative to its current location.</param>
        /// <param name="dy">The distance to move the point in the y direction relative to its current location.</param>
        /// <returns>A new point shifted on the y axis by the given delta value.</returns>
        public PdfPoint Translate(double dx, double dy)
        {
            return new PdfPoint(X + dx, Y + dy);
        }

        internal PdfVector ToVector()
        {
            return new PdfVector(X, Y);
        }

        /// <summary>
        /// Returns a value indicating whether this <see cref="PdfPoint"/> is equal to a specified <see cref="PdfPoint"/> .
        /// </summary>
        /// <param name="obj"></param>
        public override bool Equals(object obj)
        {
            if (obj is PdfPoint point)
            {
                return point.X == X && point.Y == Y;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this <see cref="PdfPoint"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return (X, Y).GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"(x:{X}, y:{Y})";
        }
    }
}
