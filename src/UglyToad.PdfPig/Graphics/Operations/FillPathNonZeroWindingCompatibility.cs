namespace UglyToad.PdfPig.Graphics.Operations
{
    using Content;

    internal class FillPathNonZeroWindingCompatibility : IGraphicsStateOperation
    {
        public const string Symbol = "F";

        public static readonly FillPathNonZeroWindingCompatibility Value = new FillPathNonZeroWindingCompatibility();

        public string Operator => Symbol;

        private FillPathNonZeroWindingCompatibility()
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