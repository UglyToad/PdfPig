namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Content;

    internal class FillPathNonZeroWindingAndStroke : IGraphicsStateOperation
    {
        public const string Symbol = "B";

        public static readonly FillPathNonZeroWindingAndStroke Value = new FillPathNonZeroWindingAndStroke();

        public string Operator => Symbol;

        private FillPathNonZeroWindingAndStroke()
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