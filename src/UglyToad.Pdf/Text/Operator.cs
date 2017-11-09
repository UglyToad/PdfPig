namespace UglyToad.Pdf.Text
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an operator which operates on operands containing the data.
    /// </summary>
    public class Operator : ITextObjectComponent
    {
        /// <summary>
        /// Always <see langword="true"/>
        /// </summary>
        public bool IsOperator { get; } = true;

        /// <summary>
        /// The ordered operand types required prior to this operator.
        /// </summary>
        public IReadOnlyList<TextObjectComponentType> OperandTypes { get; }

        /// <summary>
        /// The type of this operator.
        /// </summary>
        public TextObjectComponentType Type { get; }

        /// <summary>
        /// Always <see langword="null"/>.
        /// </summary>
        public IOperand AsOperand { get; } = null;

        public Operator(TextObjectComponentType type, IReadOnlyList<TextObjectComponentType> operandTypes)
        {
            OperandTypes = operandTypes;
            Type = type;
        }

        public override string ToString()
        {
            return $"Operator: {Type}";
        }
    }
}