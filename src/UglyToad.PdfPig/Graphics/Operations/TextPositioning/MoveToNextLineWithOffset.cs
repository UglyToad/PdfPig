namespace UglyToad.PdfPig.Graphics.Operations.TextPositioning
{
    using System.IO;
    using PdfPig.Core;

    /// <inheritdoc />
    /// <summary>
    /// Move to the start of the next line offset by Tx Ty.
    /// </summary>
    /// <remarks>
    /// Performs the following operation:
    ///            1  0  0<br />
    /// Tm = Tlm = 0  1  0  * Tlm<br />
    ///            tx ty 1
    /// </remarks>
    public class MoveToNextLineWithOffset : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Td";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The x value of the offset.
        /// </summary>
        public decimal Tx { get; }

        /// <summary>
        /// The y value of the offset.
        /// </summary>
        public decimal Ty { get; }

        /// <summary>
        /// Create a new <see cref="MoveToNextLineWithOffset"/>.
        /// </summary>
        /// <param name="tx">The x offset.</param>
        /// <param name="ty">The y offset.</param>
        public MoveToNextLineWithOffset(decimal tx, decimal ty)
        {
            Tx = tx;
            Ty = ty;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.MoveToNextLineWithOffset((double)Tx, (double)Ty);
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