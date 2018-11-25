namespace UglyToad.PdfPig.Geometry
{
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
            decimal bottom;
            decimal top;

            if (y1 <= y2)
            {
                bottom = y1;
                top = y2;
            }
            else
            {
                bottom = y2;
                top = y1;
            }

            decimal left;
            decimal right;
            if (x1 <= x2)
            {
                left = x1;
                right = x2;
            }
            else
            {
                left = x2;
                right = x1;
            }

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

        internal PdfRectangle(PdfPoint topLeft, PdfPoint topRight, PdfPoint bottomLeft, PdfPoint bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;

            BottomLeft = bottomLeft;
            BottomRight = bottomRight;

            Width = bottomRight.X - bottomLeft.X;
            Height = topLeft.Y - bottomLeft.Y;

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
