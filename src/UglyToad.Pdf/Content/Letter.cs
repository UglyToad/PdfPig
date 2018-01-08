namespace UglyToad.Pdf.Content
{
    using Geometry;

    /// <summary>
    /// A glyph or combination of glyphs (characters) drawn by a PDF content stream.
    /// </summary>
    public class Letter
    {
        /// <summary>
        /// The text for this letter or unicode character.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The lower-left position of the letter. Letters with descenders will extend below this point.
        /// </summary>
        public PdfPoint Location { get; }

        /// <summary>
        /// The width of the letter.
        /// </summary>
        public decimal Width { get; }

        /// <summary>
        /// Size as defined in the PDF file. This is not equivalent to font size in points but is relative to other font sizes on the page.
        /// </summary>
        public decimal FontSize { get; }

        /// <summary>
        /// The name of the font.
        /// </summary>
        public string FontName { get; }

        /// <summary>
        /// The size of the font in points. This is not ready for public consumption as the calculation is incorrect.
        /// </summary>
        internal decimal PointSize { get; }

        /// <summary>
        /// Create a new letter to represent some text drawn by the Tj operator.
        /// </summary>
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
