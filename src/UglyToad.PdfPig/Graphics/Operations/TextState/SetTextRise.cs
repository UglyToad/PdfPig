namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;

    /// <summary>
    /// Set text rise.
    /// </summary>
    public class SetTextRise : IGraphicsStateOperation
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
        public double Rise { get; }

        /// <summary>
        /// Create a new <see cref="SetTextRise"/>.
        /// </summary>
        /// <param name="rise">The text rise.</param>
        public SetTextRise(double rise)
        {
            Rise = rise;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetTextRise(Rise);
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