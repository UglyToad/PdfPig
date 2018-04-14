namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using Geometry;

    internal interface IGlyphDescription
    {
        bool IsSimple { get; }

        SimpleGlyphDescription SimpleGlyph { get; }

        CompositeGlyphDescription CompositeGlyph { get; }

        PdfRectangle GlyphBounds { get; }

        IGlyphDescription DeepClone();
    }
}