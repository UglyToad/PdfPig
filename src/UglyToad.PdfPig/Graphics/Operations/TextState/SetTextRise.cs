namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;
    using Content;

    /// <summary>
    /// Set text rise.
    /// </summary>
    internal class SetTextRise : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Ts";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The amount of text rise - how far to move the baseline up or down from its default location.
        /// </summary>
        public decimal Rise { get; }

        /// <summary>
        /// Create a new <see cref="SetTextRise"/>.
        /// </summary>
        /// <param name="rise">The text rise.</param>
        public SetTextRise(decimal rise)
        {
            Rise = rise;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.Rise = Rise;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText(Rise, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Rise} {Symbol}";
        }
    }
}