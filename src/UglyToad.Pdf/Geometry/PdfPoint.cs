namespace UglyToad.Pdf.Geometry
{
    public struct PdfPoint
    {
        public static PdfPoint Origin = new PdfPoint(0m, 0m);

        public decimal X { get; }

        public decimal Y { get; }

        public PdfPoint(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }

        public PdfPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public PdfPoint(double x, double y)
        {
            X = (decimal)x;
            Y = (decimal)y;
        }

        public override string ToString()
        {
            return $"(x:{X}, y:{Y})";
        }
    }
}
