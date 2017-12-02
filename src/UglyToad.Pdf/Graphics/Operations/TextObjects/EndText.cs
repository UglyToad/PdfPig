namespace UglyToad.Pdf.Graphics.Operations.TextObjects
{
    using Content;
    using Pdf.Core;

    internal class EndText : IGraphicsStateOperation
    {
        public const string Symbol = "ET";
        public static readonly EndText Value = new EndText();

        public string Operator => Symbol;

        private EndText()
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