namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
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