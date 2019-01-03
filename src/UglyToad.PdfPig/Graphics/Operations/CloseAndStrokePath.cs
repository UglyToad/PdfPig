namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Close and stroke the path.
    /// </summary>
    public class CloseAndStrokePath : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "s";

        /// <summary>
        /// The instance of the <see cref="CloseAndStrokePath"/> operation.
        /// </summary>
        public static readonly CloseAndStrokePath Value = new CloseAndStrokePath();

        /// <inheritdoc />
        public string Operator => Symbol;

        private CloseAndStrokePath()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.StrokePath(true);
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