namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <inheritdoc />
    /// <summary>
    /// Set the nonstroking color based on the current color space.
    /// </summary>
    public class SetNonStrokeColor : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "sc";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The values for the color, 1 for grayscale, 3 for RGB, 4 for CMYK.
        /// </summary>
        public IReadOnlyList<double> Operands { get; }

        /// <summary>
        /// Create a new <see cref="SetNonStrokeColor"/>.
        /// </summary>
        /// <param name="operands">The color operands.</param>
        public SetNonStrokeColor(double[] operands)
        {
            Operands = operands;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().ColorSpaceContext.SetNonStrokingColor(Operands);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            foreach (var operand in Operands)
            {
                stream.WriteDecimal(operand);
                stream.WriteWhiteSpace();
            }

            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var arguments = string.Join(" ", Operands.Select(x => x.ToString("N")));
            return $"{arguments} {Symbol}";
        }
    }
}