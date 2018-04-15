namespace UglyToad.PdfPig.Content
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
        /// Position of the bounding box.
        /// </summary>
        public PdfRectangle Rectangle { get; }

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
        internal Letter(string value, PdfRectangle rectangle, decimal fontSize, string fontName, decimal pointSize)
        {
            Value = value;
            Rectangle = rectangle;
            FontSize = fontSize;
            FontName = fontName;
            PointSize = pointSize;
        }

        /// <summary>
        /// Produces a string representation of the letter and its position.
        /// </summary>
        public override string ToString()
        {
            return $"{Rectangle} {Value} {FontName} {PointSize}";
        }
    }
}
