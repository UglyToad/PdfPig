namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;

    /// <inheritdoc />
    public class SetHorizontalScaling : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Tz";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// A number specifying the percentage of the normal width.
        /// </summary>
        public decimal Scale { get; }

        /// <summary>
        /// Create a new <see cref="SetHorizontalScaling"/>.
        /// </summary>
        /// <param name="scale">The horizontal scaling percentage.</param>
        public SetHorizontalScaling(decimal scale)
        {
            Scale = scale;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetHorizontalScaling((double)Scale);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText(Scale, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Scale} {Symbol}";
        }
    }
}