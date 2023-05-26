namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;

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
        public double HorizontalDisplacement { get; }

        /// <summary>
        /// The vertical displacement in the glyph coordinate system. Must be 0.
        /// </summary>
        public double VerticalDisplacement { get; }

        /// <summary>
        /// The lower left x coordinate of the glyph bounding box.
        /// </summary>
        public double LowerLeftX { get; }

        /// <summary>
        /// The lower left y coordinate of the glyph bounding box.
        /// </summary>
        public double LowerLeftY { get; }

        /// <summary>
        /// The upper right x coordinate of the glyph bounding box.
        /// </summary>
        public double UpperRightX { get; }

        /// <summary>
        /// The upper right y coordinate of the glyph bounding box.
        /// </summary>
        public double UpperRightY { get; }
        
        /// <summary>
        /// Create a new <see cref="Type3SetGlyphWidthAndBoundingBox"/>.
        /// </summary>
        /// <param name="horizontalDisplacement">The horizontal displacement in the glyph coordinate system.</param>
        /// <param name="verticalDisplacement">The vertical displacement in the glyph coordinate system.</param>
        /// <param name="lowerLeftX">The lower left x coordinate of the glyph bounding box.</param>
        /// <param name="lowerLeftY">The lower left y coordinate of the glyph bounding box.</param>
        /// <param name="upperRightX">The upper right x coordinate of the glyph bounding box.</param>
        /// <param name="upperRightY">The upper right y coordinate of the glyph bounding box.</param>
        public Type3SetGlyphWidthAndBoundingBox(double horizontalDisplacement, double verticalDisplacement,
            double lowerLeftX, 
            double lowerLeftY, 
            double upperRightX,
            double upperRightY)
        {
            HorizontalDisplacement = horizontalDisplacement;
            VerticalDisplacement = verticalDisplacement;
            LowerLeftX = lowerLeftX;
            LowerLeftY = lowerLeftY;
            UpperRightX = upperRightX;
            UpperRightY = upperRightY;
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
            stream.WriteDecimal(LowerLeftX);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(LowerLeftY);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(UpperRightX);
            stream.WriteWhiteSpace();
            stream.WriteNumberText(UpperRightY, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{HorizontalDisplacement} {VerticalDisplacement} {LowerLeftX} {LowerLeftY} {UpperRightX} {UpperRightY} {Symbol}";
        }
    }
}