namespace UglyToad.PdfPig.Graphics.Operations.TextPositioning
{
    using System;
    using Content;
    using PdfPig.Core;

    internal class SetTextMatrix : IGraphicsStateOperation
    {
        public const string Symbol = "Tm";

        public string Operator => Symbol;

        public decimal[] Value { get; }

        public SetTextMatrix(decimal[] value)
        {
            if (value.Length != 6)
            {
                throw new ArgumentException("Text matrix must provide 6 values. Instead got: " + value);
            }

            Value = value;
        }
        
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var newMatrix = TransformationMatrix.FromArray(Value);

            operationContext.TextMatrices.TextMatrix = newMatrix;
            operationContext.TextMatrices.TextLineMatrix = newMatrix;
        }

        public override string ToString()
        {
            return $"{Value[0]} {Value[1]} {Value[2]} {Value[3]} {Value[4]} {Value[5]} {Symbol}";
        }
    }
}