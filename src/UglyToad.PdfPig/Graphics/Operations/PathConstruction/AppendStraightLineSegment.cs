namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using PdfPig.Core;

    /// <inheritdoc />
    /// <summary>
    /// Append a straight line segment from the current point to the point (x, y).
    /// </summary>
    public class AppendStraightLineSegment : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "l";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The x coordinate of the end point of the line.
        /// </summary>
        public decimal X { get; }

        /// <summary>
        /// The y coordinate of the end point of the line.
        /// </summary>
        public decimal Y { get; }
        
        /// <summary>
        /// Create a new <see cref="AppendStraightLineSegment"/>.
        /// </summary>
        /// <param name="x">The x coordinate of the line's end point.</param>
        /// <param name="y">The y coordinate of the line's end point.</param>
        public AppendStraightLineSegment(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.LineTo((double)X, (double)Y);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Y);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteWhiteSpace();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{X} {Y} {Symbol}";
        }
    }
}