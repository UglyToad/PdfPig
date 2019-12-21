namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the text leading.
    /// </summary>
    public class SetTextLeading : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "TL";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The text leading in unscaled text space units.
        /// </summary>
        public decimal Leading { get; }

        /// <summary>
        /// Create a new <see cref="SetTextLeading"/>.
        /// </summary>
        /// <param name="leading">The text leading.</param>
        public SetTextLeading(decimal leading)
        {
            Leading = leading;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.Leading = (double)Leading;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText(Leading, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Leading} {Symbol}";
        }
    }
}