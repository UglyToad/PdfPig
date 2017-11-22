namespace UglyToad.Pdf.Graphics.Operations
{
    using Geometry;

    internal class AppendEndControlPointBezierCurve : IGraphicsStateOperation
    {
        public const string Symbol = "y";

        public string Operator => Symbol;

        public PdfPoint ControlPoint1 { get; }

        public PdfPoint End { get; }

        public AppendEndControlPointBezierCurve(decimal x1, decimal y1, decimal x3, decimal y3)
        {
            ControlPoint1 = new PdfPoint(x1, y1);
            End = new PdfPoint(x3, y3);
        }

        public override string ToString()
        {
            return $"{ControlPoint1.X} {ControlPoint1.Y} {End.X} {End.Y} {Symbol}";
        }
    }
}