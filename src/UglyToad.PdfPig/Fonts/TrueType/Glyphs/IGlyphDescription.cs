namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using Geometry;

    internal interface IGlyphDescription : IMergeableGlyph, ITransformableGlyph
    {
        bool IsSimple { get; }

        PdfRectangle Bounds { get; }

        byte[] Instructions { get; }

        int[] EndPointsOfContours { get; }

        GlyphPoint[] Points { get; }

        IGlyphDescription DeepClone();
    }

    internal interface IMergeableGlyph
    {
        IGlyphDescription Merge(IGlyphDescription glyph);
    }

    internal interface ITransformableGlyph
    {
        IGlyphDescription Transform(PdfMatrix3By2 matrix);
    }
}