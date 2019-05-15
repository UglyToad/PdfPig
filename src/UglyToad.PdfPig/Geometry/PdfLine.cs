namespace UglyToad.PdfPig.Geometry
{
    /// <summary>
    /// A line in a PDF file. 
    /// </summary>
    /// <remarks>
    /// PDF coordinates are defined with the origin at the lower left (0, 0).
    /// The Y-axis extends vertically upwards and the X-axis horizontally to the right.
    /// Unless otherwise specified on a per-page basis, units in PDF space are equivalent to a typographic point (1/72 inch).
    /// </remarks>
    public struct PdfLine
    {
        /// <summary>
        /// Length of the line.
        /// </summary>
        public decimal Length
        {
            get
            {
                decimal l = (Point1.X - Point2.X) * (Point1.X - Point2.X) + 
                    (Point1.Y - Point2.Y) * (Point1.Y - Point2.Y);
                return (decimal)System.Math.Sqrt((double)l);
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
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        public PdfLine(decimal x1, decimal y1, decimal x2, decimal y2) : this(new PdfPoint(x1, y1), new PdfPoint(x2, y2)) { }

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
    }
}
