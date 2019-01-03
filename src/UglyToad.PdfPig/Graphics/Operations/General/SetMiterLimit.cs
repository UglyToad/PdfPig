namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the miter limit in the graphics state.
    /// </summary>
    public class SetMiterLimit : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "M";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The miter limit. The miter limit imposes a maximum on the ratio of the miter length to the line width. 
        /// When the limit is exceeded, the join is converted from a miter to a bevel. 
        /// </summary>
        public decimal Limit { get; }

        /// <summary>
        /// Create a new <see cref="SetMiterLimit"/>.
        /// </summary>
        public SetMiterLimit(decimal limit)
        {
            Limit = limit;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.MiterLimit = Limit;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText(Limit, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Limit} {Symbol}";
        }
    }
}