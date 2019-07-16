namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Fill the path, using the even-odd rule to determine the region to fill.
    /// </summary>
    public class FillPathEvenOddRule : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "f*";

        /// <summary>
        /// The instance of the <see cref="FillPathEvenOddRule"/> operation.
        /// </summary>
        public static readonly FillPathEvenOddRule Value = new FillPathEvenOddRule();

        /// <inheritdoc />
        public string Operator => Symbol;

        private FillPathEvenOddRule()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.FillPath(false);
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