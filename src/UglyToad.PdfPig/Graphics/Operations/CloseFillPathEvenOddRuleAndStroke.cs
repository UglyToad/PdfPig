namespace UglyToad.PdfPig.Graphics.Operations
{
    using Content;

    internal class CloseFillPathEvenOddRuleAndStroke : IGraphicsStateOperation
    {
        public const string Symbol = "b*";

        public static readonly CloseFillPathEvenOddRuleAndStroke Value = new CloseFillPathEvenOddRuleAndStroke();

        public string Operator => Symbol;

        private CloseFillPathEvenOddRuleAndStroke()
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