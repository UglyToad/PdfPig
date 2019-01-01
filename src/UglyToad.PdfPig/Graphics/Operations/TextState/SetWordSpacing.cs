namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;
    using Content;

    /// <summary>
    /// Sets the word spacing.
    /// </summary>
    internal class SetWordSpacing : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Tw";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// Sets the width of the space ' ' character. For horizontal
        /// writing positive values increase the gap between words separated by space, for vertical writing
        /// positive values decrease the gap.
        /// </summary>
        public decimal Spacing { get; }

        /// <summary>
        /// Create a new <see cref="SetWordSpacing"/>.
        /// </summary>
        /// <param name="spacing">The word spacing.</param>
        public SetWordSpacing(decimal spacing)
        {
            Spacing = spacing;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.WordSpacing = Spacing;
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