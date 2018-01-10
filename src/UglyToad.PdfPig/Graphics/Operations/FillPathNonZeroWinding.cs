namespace UglyToad.PdfPig.Graphics.Operations
{
    using Content;

    internal class FillPathNonZeroWinding : IGraphicsStateOperation
    {
        public const string Symbol = "f";

        public static readonly FillPathNonZeroWinding Value = new FillPathNonZeroWinding();

        public string Operator => Symbol;

        private FillPathNonZeroWinding()
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