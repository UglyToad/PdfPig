namespace UglyToad.PdfPig.Graphics.Operations.InlineImages
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// End an inline image object.
    /// </summary>
    public class EndInlineImage : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "EI";

        /// <summary>
        /// The instance of the <see cref="EndInlineImage"/> operation.
        /// </summary>
        public static readonly EndInlineImage Value = new EndInlineImage();

        /// <inheritdoc />
        public string Operator => Symbol;

        private EndInlineImage()
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