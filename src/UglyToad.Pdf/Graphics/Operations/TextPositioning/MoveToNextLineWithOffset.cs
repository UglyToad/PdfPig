namespace UglyToad.Pdf.Graphics.Operations.TextPositioning
{
    using Content;
    using Core;

    internal class MoveToNextLineWithOffset : IGraphicsStateOperation
    {
        public const string Symbol = "Td";

        public string Operator => Symbol;

        public decimal Tx { get; }

        public decimal Ty { get; }

        public MoveToNextLineWithOffset(decimal tx, decimal ty)
        {
            Tx = tx;
            Ty = ty;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentTextLineMatrix = operationContext.TextMatrices.TextLineMatrix;
            
            var matrix = TransformationMatrix.FromArray(1, 0, 0, 1, Tx, Ty);

            var transformed = matrix.Multiply(currentTextLineMatrix);

            operationContext.TextMatrices.TextLineMatrix = transformed;
            operationContext.TextMatrices.TextMatrix = transformed;
        }

        public override string ToString()
        {
            return $"{Tx} {Ty} {Symbol}";
        }
    }
}