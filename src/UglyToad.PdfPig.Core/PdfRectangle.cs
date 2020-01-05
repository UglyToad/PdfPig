namespace UglyToad.PdfPig.Core
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
        public double Width => Right - Left;

        /// <summary>
        /// Height of the rectangle.
        /// </summary>
        public double Height => Top - Bottom;

        /// <summary>
        /// Area of the rectangle.
        /// </summary>
        public double Area => Width * Height;

        /// <summary>
        /// Left.
        /// </summary>
        public double Left => TopLeft.X;

        /// <summary>
        /// Top.
        /// </summary>
        public double Top => TopLeft.Y;

        /// <summary>
        /// Right.
        /// </summary>
        public double Right => BottomRight.X;

        /// <summary>
        /// Bottom.
        /// </summary>
        public double Bottom => BottomRight.Y;

        /// <summary>
        /// Create a new <see cref="PdfRectangle"/>.
        /// </summary>
        public PdfRectangle(PdfPoint point1, PdfPoint point2) : this(point1.X, point1.Y, point2.X, point2.Y) { }

        /// <summary>
        /// Create a new <see cref="PdfRectangle"/>.
        /// </summary>
        public PdfRectangle(short x1, short y1, short x2, short y2) : this((double)x1, y1, x2, y2) { }

        /// <summary>
        /// Create a new <see cref="PdfRectangle"/>.
        /// </summary>
        public PdfRectangle(double x1, double y1, double x2, double y2)
        {
            double bottom;
            double top;

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

            double left;
            double right;
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

        /// <summary>
        /// Create a new <see cref="PdfRectangle"/>.
        /// </summary>
        public PdfRectangle(PdfPoint topLeft, PdfPoint topRight, PdfPoint bottomLeft, PdfPoint bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;

            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        /// <summary>
        /// Creates a new <see cref="PdfRectangle"/> which is the current rectangle moved in the x and y directions relative to its current position by a value.
        /// </summary>
        /// <param name="dx">The distance to move the rectangle in the x direction relative to its current location.</param>
        /// <param name="dy">The distance to move the rectangle in the y direction relative to its current location.</param>
        /// <returns>A new rectangle shifted on the y axis by the given delta value.</returns>
        public PdfRectangle Translate(double dx, double dy)
        {
            return new PdfRectangle(BottomLeft.Translate(dx, dy), TopRight.Translate(dx, dy));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{TopLeft}, {Width}, {Height}]";
        }
    }
}
