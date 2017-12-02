namespace UglyToad.Pdf.Graphics.Operations.SpecialGraphicsState
{
    using System;
    using Content;
    using Pdf.Core;

    internal class ModifyCurrentTransformationMatrix : IGraphicsStateOperation
    {
        public const string Symbol = "cm";

        public string Operator => Symbol;

        public decimal[] Value { get; }

        public ModifyCurrentTransformationMatrix(decimal[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length != 6)
            {
                throw new ArgumentException("The cm operator must pass 6 numbers. Instead got: " + value);
            }
            Value = value;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var newMatrix = TransformationMatrix.FromArray(Value);

            var ctm = operationContext.GetCurrentState().CurrentTransformationMatrix;

            var newCtm = newMatrix.Multiply(ctm);

            operationContext.GetCurrentState().CurrentTransformationMatrix = newCtm;
        }

        public override string ToString()
        {
            return $"{Value[0]} {Value[1]} {Value[2]} {Value[3]} {Value[4]} {Value[5]} {Symbol}";
        }
    }
}