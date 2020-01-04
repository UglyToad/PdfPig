namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    internal interface ITransformableGlyph
    {
        IGlyphDescription Transform(CompositeTransformMatrix3By2 matrix);
    }
}