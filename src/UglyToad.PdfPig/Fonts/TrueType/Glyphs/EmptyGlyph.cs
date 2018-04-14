namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using Geometry;

    internal class EmptyGlyph : IGlyphDescription
    {
        public bool IsSimple { get; } = true;

        public SimpleGlyphDescription SimpleGlyph => new SimpleGlyphDescription(new byte[0], new int[0], new GlyphPoint[0], GlyphBounds);

        public CompositeGlyphDescription CompositeGlyph { get; } = null;

        public PdfRectangle GlyphBounds { get; }

        public EmptyGlyph(PdfRectangle glyphBounds)
        {
            GlyphBounds = glyphBounds;
        }

        public IGlyphDescription DeepClone()
        {
            return new EmptyGlyph(GlyphBounds);
        }
    }
}