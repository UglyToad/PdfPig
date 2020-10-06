namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the line width in the graphics state.
    /// </summary>
    public class SetLineWidth : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "w";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The line width.
        /// </summary>
        public decimal Width { get; }

        /// <summary>
        /// Create a new <see cref="SetLineWidth"/>.
        /// </summary>
        /// <param name="width">The line width.</param>
        public SetLineWidth(decimal width)
        {
            Width = width;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetLineWidth(Width);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText(Width, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Width} {Symbol}";
        }
    }
}