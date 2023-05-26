namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using PdfPig.Core;

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
        /// The x coordinate of the lower left corner.
        /// </summary>
        public double LowerLeftX { get; }

        /// <summary>
        /// The y coordinate of the lower left corner.
        /// </summary>
        public double LowerLeftY { get; }

        /// <summary>
        /// The width of the rectangle.
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// The height of the rectangle.
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// Create a new <see cref="AppendRectangle"/>.
        /// </summary>
        /// <param name="x">The x coordinate of the lower left corner.</param>
        /// <param name="y">The y coordinate of the lower left corner.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public AppendRectangle(double x, double y, double width, double height)
        {
            LowerLeftX = x;
            LowerLeftY = y;

            Width = width;
            Height = height;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.Rectangle(LowerLeftX, LowerLeftY, Width, Height);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(LowerLeftX);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(LowerLeftY);
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
            return $"{LowerLeftX} {LowerLeftY} {Width} {Height} {Symbol}";
        }
    }
}
