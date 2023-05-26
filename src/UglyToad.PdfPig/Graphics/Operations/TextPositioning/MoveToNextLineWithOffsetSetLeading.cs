namespace UglyToad.PdfPig.Graphics.Operations.TextPositioning
{
    using System.IO;
    using TextState;

    /// <inheritdoc />
    /// <summary>
    /// Move to the start of the next line, offset from the start of the current line by (tx, ty).
    /// This operator also sets the leading parameter in the text state.
    /// </summary>
    public class MoveToNextLineWithOffsetSetLeading : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "TD";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The x value of the offset.
        /// </summary>
        public double Tx { get; }

        /// <summary>
        /// The y value of the offset and the inverse of the leading parameter.
        /// </summary>
        public double Ty { get; }

        /// <summary>
        /// Create a new <see cref="MoveToNextLineWithOffsetSetLeading"/>.
        /// </summary>
        /// <param name="tx">The x value of the offset.</param>
        /// <param name="ty">The y value of the offset and the inverse of the leading parameter.</param>
        public MoveToNextLineWithOffsetSetLeading(double tx, double ty)
        {
            Tx = tx;
            Ty = ty;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var tlOperation = new SetTextLeading(-Ty);

            tlOperation.Run(operationContext);

            var tdOperation = new MoveToNextLineWithOffset(Tx, Ty);

            tdOperation.Run(operationContext);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(Tx);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Ty);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Tx} {Ty} {Symbol}";
        }
    }
}