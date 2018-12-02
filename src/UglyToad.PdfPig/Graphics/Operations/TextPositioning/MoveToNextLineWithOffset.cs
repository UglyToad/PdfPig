namespace UglyToad.PdfPig.Graphics.Operations.TextPositioning
{
    using System.IO;
    using Content;
    using PdfPig.Core;

    /// <summary>
    /// Move to the start of the next line offset by Tx Ty.
    /// </summary>
    /// <remarks>
    /// Performs the following operation:
    ///            1  0  0<br/>
    /// Tm = Tlm = 0  1  0  * Tlm<br/>
    ///            tx ty 1
    /// </remarks>
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
            
            var matrix = TransformationMatrix.FromValues(1, 0, 0, 1, Tx, Ty);

            var transformed = matrix.Multiply(currentTextLineMatrix);

            operationContext.TextMatrices.TextLineMatrix = transformed;
            operationContext.TextMatrices.TextMatrix = transformed;
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal(Tx);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Ty);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{Tx} {Ty} {Symbol}";
        }
    }
}