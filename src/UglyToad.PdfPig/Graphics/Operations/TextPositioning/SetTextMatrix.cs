namespace UglyToad.PdfPig.Graphics.Operations.TextPositioning
{
    using System;
    using System.IO;
    using PdfPig.Core;

    /// <inheritdoc />
    /// <summary>
    /// Set the text matrix and the text line matrix.
    /// </summary>
    public class SetTextMatrix : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Tm";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The values of the text matrix.
        /// </summary>
        public decimal[] Value { get; }

        /// <summary>
        /// Create a new <see cref="SetTextMatrix"/>.
        /// </summary>
        /// <param name="value">The values of the text matrix.</param>
        public SetTextMatrix(decimal[] value)
        {
            if (value.Length != 6)
            {
                throw new ArgumentException("Text matrix must provide 6 values. Instead got: " + value);
            }

            Value = value;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetTextMatrix(Array.ConvertAll(Value, x => (double)x));
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