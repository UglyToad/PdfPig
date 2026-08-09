namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using System.Linq;
    using Tokens;
    using Writer;

    /// <summary>
    /// Set the stroking color based on the current color space with support for Pattern, Separation, DeviceN, and ICCBased color spaces.
    /// </summary>
    public sealed class SetNonStrokeColorAdvanced : IGraphicsStateOperation
    {
        private static readonly TokenWriter TokenWriter = new TokenWriter();

        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "scn";

        private readonly double[] operands;

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The values for the color.
        /// </summary>
        public ReadOnlySpan<double> Operands => operands;

        /// <summary>
        /// The name of an entry in the Pattern subdictionary of the current resource dictionary.
        /// </summary>
        public NameToken? PatternName { get; }

        /// <summary>
        /// Create a new <see cref="SetNonStrokeColorAdvanced"/>.
        /// </summary>
        /// <param name="operands">The color operands.</param>
        public SetNonStrokeColorAdvanced(double[] operands)
        {
            this.operands = operands;
        }

        /// <summary>
        /// Create a new <see cref="SetNonStrokeColorAdvanced"/>.
        /// </summary>
        /// <param name="operands">The color operands.</param>
        /// <param name="patternName">The pattern name.</param>
        public SetNonStrokeColorAdvanced(double[] operands, NameToken patternName)
            : this(operands)
        {
            PatternName = patternName;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().ColorSpaceContext.SetNonStrokingColor(operands, PatternName);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            foreach (var operand in Operands)
            {
                stream.WriteDouble(operand);
                stream.WriteWhiteSpace();
            }

            if (PatternName is not null)
            {
                TokenWriter.WriteToken(PatternName, stream);
            }

            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var arguments = string.Join(" ", operands.Select(x => x.ToString("N")));

            if (PatternName is not null)
            {
                arguments += $" {PatternName}";
            }

            return $"{arguments} {Symbol}";
        }
    }
}