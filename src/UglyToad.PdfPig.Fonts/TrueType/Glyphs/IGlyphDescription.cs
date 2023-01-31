namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using Core;
    using System.Collections.Generic;

    internal interface IGlyphDescription : IMergeableGlyph, ITransformableGlyph
    {
        bool IsSimple { get; }

        PdfRectangle Bounds { get; }

        byte[] Instructions { get; }

        ushort[] EndPointsOfContours { get; }

        GlyphPoint[] Points { get; }

        bool IsEmpty { get; }

        bool TryGetGlyphPath(out IReadOnlyList<PdfSubpath> subpaths);

        IGlyphDescription DeepClone();
    }
}