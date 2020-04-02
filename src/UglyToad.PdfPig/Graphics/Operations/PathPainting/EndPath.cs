namespace UglyToad.PdfPig.Graphics.Operations.PathPainting
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// End path without filling or stroking.
    /// </summary>
    public class EndPath : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "n";

        /// <summary>
        /// The instance of the <see cref="EndPath"/> operation.
        /// </summary>
        public static readonly EndPath Value = new EndPath();

        /// <inheritdoc />
        public string Operator => Symbol;

        private EndPath()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.EndPath();
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