namespace UglyToad.Pdf.Graphics.Operations.TextObjects
{
    using Content;
    using Core;

    internal class BeginText : IGraphicsStateOperation
    {
        public const string Symbol = "BT";
        public static readonly BeginText Value = new BeginText();

        public string Operator => Symbol;

        private BeginText()
        {
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.TextMatrices.TextMatrix = TransformationMatrix.Identity;
            operationContext.TextMatrices.TextLineMatrix = TransformationMatrix.Identity;
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}