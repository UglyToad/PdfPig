namespace UglyToad.Pdf.Graphics.Operations
{
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

        public override string ToString()
        {
            return Symbol;
        }
    }
}