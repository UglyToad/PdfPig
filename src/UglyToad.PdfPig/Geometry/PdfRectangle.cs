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
        /// Centroid point of the rectangle.
        /// </summary>
        public PdfPoint Centroid => new PdfPoint(Left + (Right - Left) / 2, Bottom + (Top - Bottom) / 2);

        /// <summary>
        /// Width of the rectangle.
        /// </summary>
        public decimal Width => Right - Left;

        /// <summary>
        /// Height of the rectangle.
        /// </summary>
        public decimal Height => Top - Bottom;

        /// <summary>
        /// Area of the rectangle.
        /// </summary>
        public decimal Area => Width * Height;

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
        internal PdfRectangle(short x1, short y1, short x2, short y2) : this((decimal)x1, y1, x2, y2) { }

        /// <summary>
        /// Create a new <see cref="PdfRectangle"/>.
        /// </summary>
        public PdfRectangle(decimal x1, decimal y1, decimal x2, decimal y2)
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
        }

        internal PdfRectangle(PdfVector topLeft, PdfVector topRight, PdfVector bottomLeft, PdfVector bottomRight)
            : this(topLeft.ToPoint(), topRight.ToPoint(), bottomLeft.ToPoint(), bottomRight.ToPoint())
        {
        }

        internal PdfRectangle(PdfPoint topLeft, PdfPoint topRight, PdfPoint bottomLeft, PdfPoint bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;

            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }


        /// <summary>
        /// Whether two rectangles overlap.
        /// </summary>
        public bool IntersectsWith(PdfRectangle rectangle)
        {
            if (Left > rectangle.Right || rectangle.Left > Right)
            {
                return false;
            }

            if (Top < rectangle.Bottom || rectangle.Top < Bottom)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// To string override.
        /// </summary>
        public override string ToString()
        {
            return $"[{TopLeft}, {Width}, {Height}]";
        }
    }
}
