namespace UglyToad.Pdf.Graphics.Operations.PathConstruction
{
    using Geometry;

    internal class AppendDualControlPointBezierCurve : IGraphicsStateOperation
    {
        public const string Symbol = "c";

        public string Operator => Symbol;

        public PdfPoint ControlPoint1 { get; }

        public PdfPoint ControlPoint2 { get; }

        public PdfPoint End { get; }

        public AppendDualControlPointBezierCurve(decimal x1, decimal y1, decimal x2, decimal y2, decimal x3, decimal y3)
        {
            ControlPoint1 = new PdfPoint(x1, y1);
            ControlPoint2 = new PdfPoint(x2, y2);
            End = new PdfPoint(x3, y3);
        }

        public override string ToString()
        {
            return $"{ControlPoint1.X} {ControlPoint1.Y} {ControlPoint2.X} {ControlPoint2.Y} {End.X} {End.Y} {Symbol}";
        }
    }
}