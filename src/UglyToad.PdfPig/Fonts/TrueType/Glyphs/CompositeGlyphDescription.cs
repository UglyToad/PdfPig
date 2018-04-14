namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using Geometry;

    internal class CompositeGlyphDescription : IGlyphDescription
    {
        public bool IsSimple { get; } = false;

        public SimpleGlyphDescription SimpleGlyph { get; } = null;

        public CompositeGlyphDescription CompositeGlyph => this;

        public PdfRectangle GlyphBounds { get; }

        public IGlyphDescription DeepClone()
        {
            return new CompositeGlyphDescription();
        }
    }
}