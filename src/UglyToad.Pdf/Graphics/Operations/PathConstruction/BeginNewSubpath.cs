namespace UglyToad.Pdf.Graphics.Operations.PathConstruction
{
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
        }

        public override string ToString()
        {
            return $"{Point.X} {Point.Y} {Symbol}";
        }
    }
}