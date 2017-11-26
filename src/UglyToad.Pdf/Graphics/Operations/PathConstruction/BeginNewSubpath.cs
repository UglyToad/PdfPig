namespace UglyToad.Pdf.Graphics.Operations.PathConstruction
{
    using Geometry;

    internal class BeginNewSubpath : IGraphicsStateOperation
    {
        public const string Symbol = "m";

        public string Operator => Symbol;

        public PdfPoint Point { get; }

        public BeginNewSubpath(decimal x, decimal y)
        {
            Point = new PdfPoint(x, y);
        }

        public override string ToString()
        {
            return $"{Point.X} {Point.Y} {Symbol}";
        }
    }
}