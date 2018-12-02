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
        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{LowerLeft.X} {LowerLeft.Y} {Width} {Height} {Symbol}";
        }
    }
}