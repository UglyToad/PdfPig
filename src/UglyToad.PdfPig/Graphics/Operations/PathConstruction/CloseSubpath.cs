namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Close the current subpath by appending a straight line segment from the current point to the starting point of the subpath. 
    /// If the current subpath is already closed, this does nothing. 
    /// </summary>
    public class CloseSubpath : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "h";

        /// <summary>
        /// The instance of the <see cref="CloseSubpath"/> operation.
        /// </summary>
        public static readonly CloseSubpath Value = new CloseSubpath();

        /// <inheritdoc />
        public string Operator => Symbol;

        private CloseSubpath()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var point = operationContext.CloseSubpath();
            if (point.HasValue)
            {
                operationContext.CurrentPosition = point.Value;
            }
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