namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Content;

    internal class FillPathEvenOddRuleAndStroke : IGraphicsStateOperation
    {
        public const string Symbol = "B*";

        public static readonly FillPathEvenOddRuleAndStroke Value = new FillPathEvenOddRuleAndStroke();

        public string Operator => Symbol;

        private FillPathEvenOddRuleAndStroke()
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