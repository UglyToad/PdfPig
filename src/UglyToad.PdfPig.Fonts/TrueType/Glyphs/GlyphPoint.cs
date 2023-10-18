namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using UglyToad.PdfPig.Core;

    internal readonly struct GlyphPoint
    {
        public short X { get; }

        public short Y { get; }

        public bool IsOnCurve { get; }

        public bool IsEndOfContour { get; }

        public GlyphPoint(short x, short y, bool isOnCurve, bool isEndOfContour)
        {
            X = x;
            Y = y;
            IsOnCurve = isOnCurve;
            IsEndOfContour = isEndOfContour;
        }

        public override string ToString()
        {
            return $"({X}, {Y}) | {IsOnCurve} | {IsEndOfContour}";
        }
    }
}
