namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Content;
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

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.CurrentPath.BezierCurveTo(ControlPoint1.X, ControlPoint1.Y,
                End.X,
                End.Y,
                End.X,
                End.Y);
            operationContext.CurrentPosition = End;
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal(ControlPoint1.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(ControlPoint1.Y);
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
            return $"{ControlPoint1.X} {ControlPoint1.Y} {End.X} {End.Y} {Symbol}";
        }
    }
}