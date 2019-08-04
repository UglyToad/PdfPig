namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Colors;
    using Tokens;
    using Writer;

    /// <inheritdoc />
    /// <summary>
    /// Set the stroking color based on the current color space with support for Pattern, Separation, DeviceN, and ICCBased color spaces.
    /// </summary>
    public class SetNonStrokeColorAdvanced : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "scn";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The values for the color.
        /// </summary>
        public IReadOnlyList<decimal> Operands { get; }

        /// <summary>
        /// The name of an entry in the Pattern subdictionary of the current resource dictionary.
        /// </summary>
        public NameToken PatternName { get; }

        /// <summary>
        /// Create a new <see cref="SetNonStrokeColorAdvanced"/>.
        /// </summary>
        /// <param name="operands">The color operands.</param>
        public SetNonStrokeColorAdvanced(IReadOnlyList<decimal> operands)
        {
            Operands = operands;
        }

        /// <summary>
        /// Create a new <see cref="SetNonStrokeColorAdvanced"/>.
        /// </summary>
        /// <param name="operands">The color operands.</param>
        /// <param name="patternName">The pattern name.</param>
        public SetNonStrokeColorAdvanced(IReadOnlyList<decimal> operands, NameToken patternName)
        {
            Operands = operands;
            PatternName = patternName;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            if (operationContext.ColorSpaceContext.CurrentNonStrokingColorSpace.GetFamily() != ColorSpaceFamily.Device)
            {
                return;
            }

            switch (Operands.Count)
            {
                case 1:
                    operationContext.ColorSpaceContext.SetNonStrokingColorGray(Operands[0]);
                    break;
                case 3:
                    operationContext.ColorSpaceContext.SetNonStrokingColorRgb(Operands[0], Operands[1], Operands[2]);
                    break;
                case 4:
                    operationContext.ColorSpaceContext.SetNonStrokingColorCmyk(Operands[0], Operands[1], Operands[2], Operands[3]);
                    break;
                default:
                    return;
            }
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            foreach (var operand in Operands)
            {
                stream.WriteDecimal(operand);
                stream.WriteWhiteSpace();
            }

            if (PatternName != null)
            {
                TokenWriter.WriteToken(PatternName, stream);
            }

            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var arguments = string.Join(" ", Operands.Select(x => x.ToString("N")));

            if (PatternName != null)
            {
                arguments += $" {PatternName}";
            }

            return $"{arguments} {Symbol}";
        }
    }
}