namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the character spacing to a number expressed in unscaled text space units.
    /// Initial value: 0.
    /// </summary>
    public class SetCharacterSpacing : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Tc";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The character spacing.
        /// </summary>
        public decimal Spacing { get; }

        /// <summary>
        /// Create a new <see cref="SetCharacterSpacing"/>.
        /// </summary>
        /// <param name="spacing">The character spacing.</param>
        public SetCharacterSpacing(decimal spacing)
        {
            Spacing = spacing;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetCharacterSpacing((double)Spacing);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText(Spacing, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Spacing} {Symbol}";
        }
    }
}