namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Geometry;

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
        /// The end point of the line.
        /// </summary>
        public PdfPoint End { get; }

        /// <summary>
        /// Create a new <see cref="AppendStraightLineSegment"/>.
        /// </summary>
        /// <param name="x">The x coordinate of the line's end point.</param>
        /// <param name="y">The y coordinate of the line's end point.</param>
        public AppendStraightLineSegment(decimal x, decimal y)
        {
            End = new PdfPoint(x, y);
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var endPoint = operationContext.CurrentTransformationMatrix.Transform(new PdfPoint(End.X, End.Y));
            operationContext.CurrentPath.LineTo(endPoint.X, endPoint.Y);
            operationContext.CurrentPosition = endPoint;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(End.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(End.Y);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteWhiteSpace();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{End.X} {End.Y} {Symbol}";
        }
    }
}