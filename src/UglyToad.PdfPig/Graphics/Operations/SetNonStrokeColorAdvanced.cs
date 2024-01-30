namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Tokens;
    using Writer;

    /// <summary>
    /// Set the stroking color based on the current color space with support for Pattern, Separation, DeviceN, and ICCBased color spaces.
    /// </summary>
    public class SetNonStrokeColorAdvanced : IGraphicsStateOperation
    {
        private static readonly TokenWriter TokenWriter = new TokenWriter();

        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "scn";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The values for the color.
        /// </summary>
        public IReadOnlyList<double> Operands { get; }

        /// <summary>
        /// The name of an entry in the Pattern subdictionary of the current resource dictionary.
        /// </summary>
        public NameToken PatternName { get; }

        /// <summary>
        /// Create a new <see cref="SetNonStrokeColorAdvanced"/>.
        /// </summary>
        /// <param name="operands">The color operands.</param>
        public SetNonStrokeColorAdvanced(IReadOnlyList<double> operands)
        {
            Operands = operands;
        }

        /// <summary>
        /// Create a new <see cref="SetNonStrokeColorAdvanced"/>.
        /// </summary>
        /// <param name="operands">The color operands.</param>
        /// <param name="patternName">The pattern name.</param>
        public SetNonStrokeColorAdvanced(IReadOnlyList<double> operands, NameToken patternName)
        {
            Operands = operands;
            PatternName = patternName;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().ColorSpaceContext.SetNonStrokingColor(Operands, PatternName);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            foreach (var operand in Operands)
            {
                stream.WriteDouble(operand);
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