namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;
    using Geometry;

    /// <inheritdoc />
    /// <summary>
    /// Set width information for the glyph and declare that the glyph description specifies both its shape and its color for a Type 3 font.
    /// Also sets the glyph bounding box.
    /// </summary>
    public class Type3SetGlyphWidthAndBoundingBox : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "d1";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The horizontal displacement in the glyph coordinate system.
        /// </summary>
        public decimal HorizontalDisplacement { get; }

        /// <summary>
        /// The vertical displacement in the glyph coordinate system. Must be 0.
        /// </summary>
        public decimal VerticalDisplacement { get; }

        /// <summary>
        /// The glyph bounding box.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// Create a new <see cref="Type3SetGlyphWidthAndBoundingBox"/>.
        /// </summary>
        /// <param name="horizontalDisplacement">The horizontal displacement in the glyph coordinate system.</param>
        /// <param name="verticalDisplacement">The vertical displacement in the glyph coordinate system.</param>
        /// <param name="lowerLeftX">The lower left x coordinate of the glyph bounding box.</param>
        /// <param name="lowerLeftY">The lower left y coordinate of the glyph bounding box.</param>
        /// <param name="upperRightX">The upper right x coordinate of the glyph bounding box.</param>
        /// <param name="upperRightY">The upper right y coordinate of the glyph bounding box.</param>
        public Type3SetGlyphWidthAndBoundingBox(decimal horizontalDisplacement, decimal verticalDisplacement,
            decimal lowerLeftX, 
            decimal lowerLeftY, 
            decimal upperRightX,
            decimal upperRightY)
        {
            HorizontalDisplacement = horizontalDisplacement;
            VerticalDisplacement = verticalDisplacement;
            BoundingBox = new PdfRectangle(new PdfPoint(lowerLeftX, lowerLeftY), new PdfPoint(upperRightX, upperRightY));
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(HorizontalDisplacement);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(VerticalDisplacement);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(BoundingBox.Left);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(BoundingBox.Bottom);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(BoundingBox.Right);
            stream.WriteWhiteSpace();
            stream.WriteNumberText(BoundingBox.Top, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{HorizontalDisplacement} {VerticalDisplacement} {BoundingBox.Left} {BoundingBox.Bottom} {BoundingBox.Right} {BoundingBox.Top} {Symbol}";
        }
    }
}