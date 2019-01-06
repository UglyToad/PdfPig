namespace UglyToad.PdfPig.Graphics.Operations.InlineImages
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Begin the image data for an inline image object. 
    /// </summary>
    public class BeginInlineImageData : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "ID";

        /// <summary>
        /// The instance of the <see cref="BeginInlineImageData"/> operation.
        /// </summary>
        public static readonly BeginInlineImageData Value = new BeginInlineImageData();

        /// <inheritdoc />
        public string Operator => Symbol;

        private BeginInlineImageData()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
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