namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Content;
    using Geometry;

    internal class AppendStartControlPointBezierCurve : IGraphicsStateOperation
    {
        public const string Symbol = "v";

        public string Operator => Symbol;

        public PdfPoint ControlPoint2 { get; }

        public PdfPoint End { get; }

        public AppendStartControlPointBezierCurve(decimal x2, decimal y2, decimal x3, decimal y3)
        {
            ControlPoint2 = new PdfPoint(x2, y2);
            End = new PdfPoint(x3, y3);
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{ControlPoint2.X} {ControlPoint2.Y} {End.X} {End.Y} {Symbol}";
        }
    }
}