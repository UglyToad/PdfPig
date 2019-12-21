namespace UglyToad.PdfPig.Fonts
{
    using Geometry;

    internal class CharacterBoundingBox
    {
        public PdfRectangle GlyphBounds { get; }

        public double Width { get; }

        public CharacterBoundingBox(PdfRectangle glyphBounds, double width)
        {
            GlyphBounds = glyphBounds;
            Width = width;
        }
    }
}