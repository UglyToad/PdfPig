namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using Geometry;

    internal struct GlyphPoint
    {
        public PdfPoint Point { get; }

        public bool IsOnCurve { get; }

        public GlyphPoint(decimal x, decimal y, bool isOnCurve) : this(new PdfPoint(x, y), isOnCurve) { }
        public GlyphPoint(PdfPoint point, bool isOnCurve)
        {
            Point = point;
            IsOnCurve = isOnCurve;
        }

        public override string ToString()
        {
            return $"{Point} | {IsOnCurve}";
        }
    }
}
