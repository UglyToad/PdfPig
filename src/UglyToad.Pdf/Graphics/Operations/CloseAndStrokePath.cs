namespace UglyToad.Pdf.Graphics.Operations
{
    using Content;

    internal class CloseAndStrokePath : IGraphicsStateOperation
    {
        public const string Symbol = "s";

        public static readonly CloseAndStrokePath Value = new CloseAndStrokePath();

        public string Operator => Symbol;

        private CloseAndStrokePath()
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