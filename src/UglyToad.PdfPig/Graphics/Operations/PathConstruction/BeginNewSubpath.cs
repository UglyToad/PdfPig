namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Content;
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

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.BeginSubpath();
            operationContext.CurrentPosition = Point;
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal(Point.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Point.Y);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{Point.X} {Point.Y} {Symbol}";
        }
    }
}