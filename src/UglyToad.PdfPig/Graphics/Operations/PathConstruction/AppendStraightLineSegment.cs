namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Content;
    using Geometry;

    internal class AppendStraightLineSegment : IGraphicsStateOperation
    {
        public const string Symbol = "l";

        public string Operator => Symbol;

        public PdfPoint End { get; }

        public AppendStraightLineSegment(decimal x, decimal y)
        {
            End = new PdfPoint(x, y);
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.CurrentPath.LineTo(End.X, End.Y);
            operationContext.CurrentPosition = End;
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal(End.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(End.Y);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteWhiteSpace();
        }

        public override string ToString()
        {
            return $"{End.X} {End.Y} {Symbol}";
        }
    }
}