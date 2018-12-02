namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Content;

    internal class CloseFillPathNonZeroWindingAndStroke : IGraphicsStateOperation
    {
        public const string Symbol = "b";

        public static readonly CloseFillPathNonZeroWindingAndStroke Value = new CloseFillPathNonZeroWindingAndStroke();

        public string Operator => Symbol;

        private CloseFillPathNonZeroWindingAndStroke()
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