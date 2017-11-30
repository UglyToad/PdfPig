namespace UglyToad.Pdf.Graphics.Operations.ClippingPaths
{
    using Content;

    internal class ModifyClippingByNonZeroWindingIntersect : IGraphicsStateOperation
    {
        public const string Symbol = "W";

        public static readonly ModifyClippingByNonZeroWindingIntersect Value = new ModifyClippingByNonZeroWindingIntersect();

        public string Operator => Symbol;

        private ModifyClippingByNonZeroWindingIntersect()
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