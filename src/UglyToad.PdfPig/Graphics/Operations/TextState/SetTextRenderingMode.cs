namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using Core;
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the text rendering mode.
    /// </summary>
    public class SetTextRenderingMode : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Tr";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The text rendering mode to set.
        /// </summary>
        public TextRenderingMode Mode { get; }

        /// <summary>
        /// Create a new <see cref="SetTextRenderingMode"/>.
        /// </summary>
        public SetTextRenderingMode(int mode)
        {
            Mode = (TextRenderingMode)mode;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetTextRenderingMode(Mode);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText((int)Mode, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Mode} {Symbol}";
        }
    }
}