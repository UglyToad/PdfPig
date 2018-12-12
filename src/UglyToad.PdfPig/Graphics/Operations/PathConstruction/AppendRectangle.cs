namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Content;
    using Geometry;

    internal class AppendRectangle : IGraphicsStateOperation
    {
        public const string Symbol = "re";

        public string Operator => Symbol;

        public PdfPoint LowerLeft { get; }

        public decimal Width { get; }

        public decimal Height { get; }

        public AppendRectangle(decimal x, decimal y, decimal width, decimal height)
        {
            LowerLeft = new PdfPoint(x, y);

            Width = width;
            Height = height;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.BeginSubpath();
            operationContext.CurrentPath.Rectangle(LowerLeft.X, LowerLeft.Y, Width, Height);
            operationContext.CurrentPath.ClosePath();
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal(LowerLeft.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(LowerLeft.Y);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Width);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Height);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{LowerLeft.X} {LowerLeft.Y} {Width} {Height} {Symbol}";
        }
    }
}