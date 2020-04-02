namespace UglyToad.PdfPig.Graphics.Operations.PathPainting
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Stroke the path.
    /// </summary>
    public class StrokePath : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "S";

        /// <summary>
        /// The instance of the <see cref="StrokePath"/> operation.
        /// </summary>
        public static readonly StrokePath Value = new StrokePath();

        /// <inheritdoc />
        public string Operator => Symbol;

        private StrokePath()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.StrokePath(false);
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