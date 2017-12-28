namespace UglyToad.Pdf.Content
{
    using Geometry;

    public class Letter
    {
        /// <summary>
        /// The text for this letter or unicode character.
        /// </summary>
        public string Value { get; }

        public PdfPoint Location { get; }

        /// <summary>
        /// The width of the letter.
        /// </summary>
        public decimal Width { get; }

        /// <summary>
        /// Size defined by the Tj operator prior to our possibly incorrect transformation.
        /// </summary>
        internal decimal FontSize { get; }

        /// <summary>
        /// The name of the font.
        /// </summary>
        public string FontName { get; }

        /// <summary>
        /// The size of the font in points.
        /// </summary>
        public decimal PointSize { get; }

        internal Letter(string value, PdfPoint location, decimal width, decimal fontSize, string fontName, decimal pointSize)
        {
            Value = value;
            Location = location;
            Width = width;
            FontSize = fontSize;
            FontName = fontName;
            PointSize = pointSize;
        }

        public override string ToString()
        {
            return $"{Location} {Width} {Value} {FontName} {PointSize}";
        }
    }
}
