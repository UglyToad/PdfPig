namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using Geometry;

    internal interface IGlyphDescription
    {
        bool IsSimple { get; }

        SimpleGlyphDescription SimpleGlyph { get; }

        object CompositeGlyph { get; }

        PdfRectangle GlyphBounds { get; }
    }
}