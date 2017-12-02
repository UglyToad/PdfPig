namespace UglyToad.Pdf.Geometry
{
    internal struct PdfVector
    {
        public decimal X { get; }

        public decimal Y { get; }

        public PdfVector(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }

        public PdfVector Scale(decimal scale)
        {
            return new PdfVector(X * scale, Y * scale);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
