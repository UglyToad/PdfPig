namespace UglyToad.Pdf.Graphics.Operations
{
    using Content;

    internal class FillPathEvenOddRule : IGraphicsStateOperation
    {
        public const string Symbol = "f*";

        public static readonly FillPathEvenOddRule Value = new FillPathEvenOddRule();

        public string Operator => Symbol;

        private FillPathEvenOddRule()
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