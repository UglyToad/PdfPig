namespace UglyToad.PdfPig.Graphics.Operations.TextPositioning
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Move to the start of the next line.
    /// </summary>
    /// <remarks>
    /// This performs this operation: 0 -Tl Td
    /// The offset is negative leading text (Tl) value, this is incorrect in the specification.
    /// </remarks>
    public class MoveToNextLine : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "T*";

        /// <summary>
        /// The instance of the <see cref="MoveToNextLine"/> operation.
        /// </summary>
        public static readonly MoveToNextLine Value = new MoveToNextLine();

        /// <inheritdoc />
        public string Operator => Symbol;

        private MoveToNextLine()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var tdOperation = new MoveToNextLineWithOffset(0, -1 * operationContext.GetCurrentState().FontState.Leading);

            tdOperation.Run(operationContext);
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