namespace UglyToad.PdfPig.Graphics.Operations.ClippingPaths
{
    using System.IO;
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