namespace UglyToad.PdfPig.Geometry
{
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
        public static PdfPoint Origin = new PdfPoint(0m, 0m);

        /// <summary>
        /// The X coordinate for this point. (Horizontal axis).
        /// </summary>
        public decimal X { get; }

        /// <summary>
        /// The Y coordinate of this point. (Vertical axis).
        /// </summary>
        public decimal Y { get; }

        /// <summary>
        /// Create a new <see cref="PdfPoint"/> at this position.
        /// </summary>
        public PdfPoint(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Create a new <see cref="PdfPoint"/> at this position.
        /// </summary>
        public PdfPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Create a new <see cref="PdfPoint"/> at this position.
        /// </summary>
        public PdfPoint(double x, double y)
        {
            X = (decimal)x;
            Y = (decimal)y;
        }

        internal PdfVector ToVector()
        {
            return new PdfVector(X, Y);
        }

        /// <summary>
        /// Get a string representation of this point.
        /// </summary>
        public override string ToString()
        {
            return $"(x:{X}, y:{Y})";
        }
    }
}
