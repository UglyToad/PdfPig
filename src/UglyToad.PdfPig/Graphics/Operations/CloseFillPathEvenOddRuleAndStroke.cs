namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
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

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}