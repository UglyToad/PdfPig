namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    internal interface IMergeableGlyph
    {
        IGlyphDescription Merge(IGlyphDescription glyph);
    }
}