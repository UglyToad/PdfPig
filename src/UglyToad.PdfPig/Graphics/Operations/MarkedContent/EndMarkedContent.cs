namespace UglyToad.PdfPig.Graphics.Operations.MarkedContent
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// End a marked-content sequence.
    /// </summary>
    public class EndMarkedContent : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "EMC";

        /// <summary>
        /// The instance of the <see cref="EndMarkedContent"/> operation.
        /// </summary>
        public static readonly EndMarkedContent Value = new EndMarkedContent();

        /// <inheritdoc />
        public string Operator => Symbol;
        
        private EndMarkedContent()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.EndMarkedContent();
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