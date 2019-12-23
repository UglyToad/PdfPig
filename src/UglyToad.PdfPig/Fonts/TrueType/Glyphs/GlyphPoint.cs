namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    internal struct GlyphPoint
    {
        public short X { get; }

        public short Y { get; }

        public bool IsOnCurve { get; }

        public GlyphPoint(short x, short y, bool isOnCurve) 
        {
            X = x;
            Y = y;
            IsOnCurve = isOnCurve;
        }

        public override string ToString()
        {
            return $"({X}, {Y}) | {IsOnCurve}";
        }
    }
}
