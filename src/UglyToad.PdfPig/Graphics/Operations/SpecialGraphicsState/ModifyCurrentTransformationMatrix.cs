namespace UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState
{
    using System;
    using System.IO;
    using PdfPig.Core;

    /// <inheritdoc />
    /// <summary>
    /// Modify the current transformation matrix by concatenating the specified matrix. 
    /// </summary>
    public class ModifyCurrentTransformationMatrix : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "cm";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The 6 values for the transformation matrix.
        /// </summary>
        public decimal[] Value { get; }

        /// <summary>
        /// Create a new <see cref="ModifyCurrentTransformationMatrix"/>.
        /// </summary>
        /// <param name="value">The 6 transformation matrix values.</param>
        public ModifyCurrentTransformationMatrix(decimal[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length != 6)
            {
                throw new ArgumentException("The cm operator must pass 6 numbers. Instead got: " + value.Length);
            }
            Value = value;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.ModifyCurrentTransformationMatrix(Array.ConvertAll(Value, x => (double)x));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Value[0]} {Value[1]} {Value[2]} {Value[3]} {Value[4]} {Value[5]} {Symbol}";
        }
    }
}