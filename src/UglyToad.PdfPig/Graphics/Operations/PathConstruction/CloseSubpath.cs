namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
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
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}