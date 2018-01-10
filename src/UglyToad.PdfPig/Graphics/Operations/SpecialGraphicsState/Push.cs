namespace UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState
{
    using Content;

    internal class Push : IGraphicsStateOperation
    {
        public const string Symbol = "q";
        public static readonly Push Value = new Push();

        public string Operator => Symbol;

        private Push()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }

        public void Run(IOperationContext context, IResourceStore resourceStore)
        {
            context.PushState();
        }
    }
}
