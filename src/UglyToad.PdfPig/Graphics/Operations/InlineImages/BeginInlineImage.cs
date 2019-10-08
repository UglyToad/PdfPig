namespace UglyToad.PdfPig.Graphics.Operations.InlineImages
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Begin an inline image object.
    /// </summary>
    public class BeginInlineImage : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "BI";

        /// <summary>
        /// The instance of the <see cref="BeginInlineImage"/> operation.
        /// </summary>
        public static readonly BeginInlineImage Value = new BeginInlineImage();

        /// <inheritdoc />
        public string Operator => Symbol;

        private BeginInlineImage()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.BeginInlineImage();
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
