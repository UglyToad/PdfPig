namespace UglyToad.PdfPig.Graphics.Operations.TextObjects
{
    using System.IO;
    using PdfPig.Core;

    /// <inheritdoc />
    /// <summary>
    /// Begin a text object, initializing the text matrix and the text line matrix to the identity matrix. Text objects cannot be nested.
    /// </summary>
    public class BeginText : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "BT";

        /// <summary>
        /// The instance of the <see cref="BeginText"/> operation.
        /// </summary>
        public static readonly BeginText Value = new BeginText();

        /// <inheritdoc />
        public string Operator => Symbol;

        private BeginText()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.BeginText();
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