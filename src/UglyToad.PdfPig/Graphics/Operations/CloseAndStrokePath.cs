namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Content;

    /// <summary>
    /// Close and stroke the path.
    /// </summary>
    internal class CloseAndStrokePath : IGraphicsStateOperation
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
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
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