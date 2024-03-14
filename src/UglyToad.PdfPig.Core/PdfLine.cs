namespace UglyToad.PdfPig.Core
{
    using System;

    /// <summary>
    /// A line in a PDF file.
    /// </summary>
    /// <remarks>
    /// PDF coordinates are defined with the origin at the lower left (0, 0).
    /// The Y-axis extends vertically upwards and the X-axis horizontally to the right.
    /// Unless otherwise specified on a per-page basis, units in PDF space are equivalent to a typographic point (1/72 inch).
    /// </remarks>
    public readonly struct PdfLine
    {
        /// <summary>
        /// Length of the line.
        /// </summary>
        public double Length
        {
            get
            {
                var dx = Point1.X - Point2.X;
                var dy = Point1.Y - Point2.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        /// <summary>
        /// First point of the line.
        /// </summary>
        public PdfPoint Point1 { get; }

        /// <summary>
        /// Second point of the line.
        /// </summary>
        public PdfPoint Point2 { get; }

        /// <summary>
        /// Create a new <see cref="PdfLine"/>.
        /// </summary>
        /// <param name="x1">The x coordinate of the first point on the line.</param>
        /// <param name="y1">The y coordinate of the first point on the line.</param>
        /// <param name="x2">The x coordinate of the second point on the line.</param>
        /// <param name="y2">The y coordinate of the second point on the line.</param>
        public PdfLine(double x1, double y1, double x2, double y2) : this(new PdfPoint(x1, y1), new PdfPoint(x2, y2)) { }

        /// <summary>
        /// Create a new <see cref="PdfLine"/>.
        /// </summary>
        /// <param name="point1">First point of the line.</param>
        /// <param name="point2">Second point of the line.</param>
        public PdfLine(PdfPoint point1, PdfPoint point2)
        {
            Point1 = point1;
            Point2 = point2;
        }

        /// <summary>
        /// The rectangle completely containing the <see cref="PdfLine"/>.
        /// </summary>
        public PdfRectangle GetBoundingRectangle()
        {
            return new PdfRectangle(
                Math.Min(Point1.X, Point2.X),
                Math.Min(Point1.Y, Point2.Y),
                Math.Max(Point1.X, Point2.X),
                Math.Max(Point1.Y, Point2.Y));
        }

        /// <summary>
        /// Returns a value indicating whether this <see cref="PdfLine"/> is equal to a specified <see cref="PdfLine"/> .
        /// </summary>
        /// <param name="obj"></param>
        public override bool Equals(object obj)
        {
            if (obj is PdfLine line)
            {
                return line.Point1.Equals(Point1) && line.Point2.Equals(Point2);
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this <see cref="PdfLine"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Point1, Point2);
        }
    }
}
