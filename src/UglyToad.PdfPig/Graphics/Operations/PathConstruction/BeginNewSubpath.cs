namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Geometry;

    /// <inheritdoc />
    /// <summary>
    /// Begin a new subpath by moving the current point to coordinates (x, y), omitting any connecting line segment.
    /// </summary>
    public class BeginNewSubpath : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "m";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The point to begin the new subpath at.
        /// </summary>
        public PdfPoint Point { get; }

        /// <summary>
        /// Create a new <see cref="BeginNewSubpath"/>.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        public BeginNewSubpath(decimal x, decimal y)
        {
            Point = new PdfPoint(x, y);
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.BeginSubpath();
            operationContext.CurrentPosition = Point;
            operationContext.CurrentPath.LineTo(Point.X, Point.Y);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(Point.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Point.Y);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Point.X} {Point.Y} {Symbol}";
        }
    }
}