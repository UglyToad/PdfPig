namespace UglyToad.PdfPig.Fonts
{
    using Geometry;

    internal class CharacterBoundingBox
    {
        public PdfRectangle GlyphBounds { get; }

        public decimal Width { get; }

        public CharacterBoundingBox(PdfRectangle glyphBounds, decimal width)
        {
            GlyphBounds = glyphBounds;
            Width = width;
        }
    }
}