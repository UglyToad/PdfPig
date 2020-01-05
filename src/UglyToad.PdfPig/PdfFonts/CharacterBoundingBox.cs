namespace UglyToad.PdfPig.PdfFonts
{
    using Core;

    internal class CharacterBoundingBox
    {
        public PdfRectangle GlyphBounds { get; }

        public double Width { get; }

        public CharacterBoundingBox(PdfRectangle bounds, double width)
        {
            GlyphBounds = bounds;
            Width = width;
        }
    }
}
