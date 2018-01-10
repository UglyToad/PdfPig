namespace UglyToad.PdfPig.Graphics.Operations.ClippingPaths
{
    using Content;

    internal class ModifyClippingByEvenOddIntersect : IGraphicsStateOperation
    {
        public const string Symbol = "W*";

        public static readonly ModifyClippingByEvenOddIntersect Value = new ModifyClippingByEvenOddIntersect();

        public string Operator => Symbol;

        private ModifyClippingByEvenOddIntersect()
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