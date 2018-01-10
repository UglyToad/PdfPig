namespace UglyToad.PdfPig.Graphics.Operations
{
    using Content;

    internal class EndPath : IGraphicsStateOperation
    {
        public const string Symbol = "n";

        public static readonly EndPath Value = new EndPath();

        public string Operator => Symbol;

        private EndPath()
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