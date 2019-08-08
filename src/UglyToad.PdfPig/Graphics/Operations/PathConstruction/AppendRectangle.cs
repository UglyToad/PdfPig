namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Geometry;

    /// <inheritdoc />
    /// <remarks>
    /// Append a rectangle to the current path as a complete subpath.
    /// </remarks>
    public class AppendRectangle : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "re";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The lower left corner.
        /// </summary>
        public PdfPoint LowerLeft { get; }

        /// <summary>
        /// The width of the rectangle.
        /// </summary>
        public decimal Width { get; }

        /// <summary>
        /// The height of the rectangle.
        /// </summary>
        public decimal Height { get; }

        /// <summary>
        /// Create a new <see cref="AppendRectangle"/>.
        /// </summary>
        /// <param name="x">The x coordinate of the lower left corner.</param>
        /// <param name="y">The y coordinate of the lower left corner.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public AppendRectangle(decimal x, decimal y, decimal width, decimal height)
        {
            LowerLeft = new PdfPoint(x, y);

            Width = width;
            Height = height;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.BeginSubpath();
            var lowerLeftTransform = operationContext.CurrentTransformationMatrix.Transform(LowerLeft);
            operationContext.CurrentPath.Rectangle(lowerLeftTransform.X, lowerLeftTransform.Y, Width, Height);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(LowerLeft.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(LowerLeft.Y);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Width);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Height);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{LowerLeft.X} {LowerLeft.Y} {Width} {Height} {Symbol}";
        }
    }
}