namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Set the nonstroking color based on the current color space.
    /// </summary>
    public sealed class SetNonStrokeColor : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "sc";

        private readonly double[] operands;

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The values for the color, 1 for grayscale, 3 for RGB, 4 for CMYK.
        /// </summary>
        public ReadOnlySpan<double> Operands => operands;

        /// <summary>
        /// Create a new <see cref="SetNonStrokeColor"/>.
        /// </summary>
        /// <param name="operands">The color operands.</param>
        public SetNonStrokeColor(double[] operands)
        {
            this.operands = operands;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().ColorSpaceContext.SetNonStrokingColor(operands);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            foreach (var operand in Operands)
            {
                stream.WriteDouble(operand);
                stream.WriteWhiteSpace();
            }

            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var arguments = string.Join(" ", operands.Select(x => x.ToString("N")));
            return $"{arguments} {Symbol}";
        }
    }
}