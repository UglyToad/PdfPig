namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Content;

    internal class CloseSubpath : IGraphicsStateOperation
    {
        public const string Symbol = "h";

        public static readonly CloseSubpath Value = new CloseSubpath();

        public string Operator => Symbol;

        private CloseSubpath()
        {
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.CurrentPath.ClosePath();
        }

        public void Write(Stream stream)
        {
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}