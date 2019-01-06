namespace UglyToad.PdfPig.Graphics.Operations.Compatibility
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// End a compatibility section.
    /// </summary>
    public class EndCompatibilitySection : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "EX";

        /// <summary>
        /// The instance of the <see cref="EndCompatibilitySection"/> operation.
        /// </summary>
        public static readonly EndCompatibilitySection Value = new EndCompatibilitySection();

        /// <inheritdoc />
        public string Operator => Symbol;

        private EndCompatibilitySection()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Symbol;
        }
    }
}