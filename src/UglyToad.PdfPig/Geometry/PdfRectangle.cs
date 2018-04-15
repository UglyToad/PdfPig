namespace UglyToad.PdfPig.Geometry
{
    using System;

    /// <summary>
    /// A rectangle in a PDF file. 
    /// </summary>
    /// <remarks>
    /// PDF coordinates are defined with the origin at the lower left (0, 0).
    /// The Y-axis extends vertically upwards and the X-axis horizontally to the right.
    /// Unless otherwise specified on a per-page basis, units in PDF space are equivalent to a typographic point (1/72 inch).
    /// </remarks>
    public struct PdfRectangle
    {
        /// <summary>
        /// Top left point of the rectangle.
        /// </summary>
        public PdfPoint TopLeft { get; }

        /// <summary>
        /// Top right point of the rectangle.
        /// </summary>
        public PdfPoint TopRight { get; }

        /// <summary>
        /// Bottom right point of the rectangle.
        /// </summary>
        public PdfPoint BottomRight { get; }

        /// <summary>
        /// Bottom left point of the rectangle.
        /// </summary>
        public PdfPoint BottomLeft { get; }

        /// <summary>
        /// Width of the rectangle.
        /// </summary>
        public decimal Width { get; }

        /// <summary>
        /// Height of the rectangle.
        /// </summary>
        public decimal Height { get; }

        /// <summary>
        /// Area of the rectangle.
        /// </summary>
        public decimal Area { get; }

        /// <summary>
        /// Left.
        /// </summary>
        public decimal Left => TopLeft.X;

        /// <summary>
        /// Top.
        /// </summary>
        public decimal Top => TopLeft.Y;

        /// <summary>
        /// Right.
        /// </summary>
        public decimal Right => BottomRight.X;

        /// <summary>
        /// Bottom.
        /// </summary>
        public decimal Bottom => BottomRight.Y;

        internal PdfRectangle(PdfPoint point1, PdfPoint point2) : this(point1.X, point1.Y, point2.X, point2.Y) { }
        internal PdfRectangle(short x1, short y1, short x2, short y2) : this((decimal) x1, y1, x2, y2) { }
        internal PdfRectangle(decimal x1, decimal y1, decimal x2, decimal y2)
        {
            var bottom = Math.Min(y1, y2);
            var top = Math.Max(y1, y2);

            var left = Math.Min(x1, x2);
            var right = Math.Max(x1, x2);

            TopLeft = new PdfPoint(left, top);
            TopRight = new PdfPoint(right, top);

            BottomLeft = new PdfPoint(left, bottom);
            BottomRight = new PdfPoint(right, bottom);

            Width = right - left;
            Height = top - bottom;

            Area = Width * Height;
        }

        internal PdfRectangle(PdfVector topLeft, PdfVector topRight, PdfVector bottomLeft, PdfVector bottomRight)
        {
            TopLeft = topLeft.ToPoint();
            TopRight = topRight.ToPoint();

            BottomLeft = bottomLeft.ToPoint();
            BottomRight = bottomRight.ToPoint();

            Width = bottomRight.Subtract(bottomLeft).GetMagnitude();
            Height = topLeft.Subtract(bottomLeft).GetMagnitude();

            Area = Width * Height;
        }

        /// <summary>
        /// To string override.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[{TopLeft}, {Width}, {Height}]";
        }
    }
}
