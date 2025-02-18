namespace UglyToad.PdfPig.PdfFonts
{
    using Core;

    /// <summary>
    /// Character bounding box.
    /// </summary>
    public sealed class CharacterBoundingBox
    {
        /// <summary>
        /// The glyph bounds.
        /// </summary>
        public PdfRectangle GlyphBounds { get; }

        /// <summary>
        /// The width.
        /// </summary>
        public double Width { get; }

        internal CharacterBoundingBox(PdfRectangle bounds, double width)
        {
            GlyphBounds = bounds;
            Width = width;
        }
    }
}
