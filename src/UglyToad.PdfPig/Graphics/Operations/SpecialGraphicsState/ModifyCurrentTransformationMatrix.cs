namespace UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState
{
    using System;
    using System.IO;
    using Content;
    using PdfPig.Core;

    /// <summary>
    /// Modify the current transformation matrix by concatenating the specified matrix. 
    /// </summary>
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

        public void Write(Stream stream)
        {
            stream.WriteDecimal(Value[0]);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Value[1]);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Value[2]);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Value[3]);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Value[4]);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Value[5]);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{Value[0]} {Value[1]} {Value[2]} {Value[3]} {Value[4]} {Value[5]} {Symbol}";
        }
    }
}