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
            operationContext.CurrentPath.BezierCurveTo(operationContext.CurrentPosition.X,
                operationContext.CurrentPosition.Y,
                ControlPoint2.X,
                ControlPoint2.Y,
                End.X,
                End.Y);
            operationContext.CurrentPosition = End;
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal(ControlPoint2.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(ControlPoint2.Y);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(End.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(End.Y);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{ControlPoint2.X} {ControlPoint2.Y} {End.X} {End.Y} {Symbol}";
        }
    }
}