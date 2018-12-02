namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
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