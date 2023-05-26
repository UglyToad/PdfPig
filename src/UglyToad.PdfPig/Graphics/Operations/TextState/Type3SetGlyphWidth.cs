namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set width information for the glyph and declare that the glyph description specifies both its shape and its color for a Type 3 font.
    /// wx specifies the horizontal displacement in the glyph coordinate system; 
    /// it must be consistent with the corresponding width in the font's Widths array.
    /// wy must be 0. 
    /// </summary>
    public class Type3SetGlyphWidth : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "d0";

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
        /// Create a new <see cref="Type3SetGlyphWidth"/>.
        /// </summary>
        /// <param name="horizontalDisplacement">The horizontal displacement in the glyph coordinate system.</param>
        /// <param name="verticalDisplacement">The vertical displacement in the glyph coordinate system.</param>
        public Type3SetGlyphWidth(double horizontalDisplacement, double verticalDisplacement)
        {
            HorizontalDisplacement = horizontalDisplacement;
            VerticalDisplacement = verticalDisplacement;
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
            stream.WriteNumberText(VerticalDisplacement, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{HorizontalDisplacement} {VerticalDisplacement} {Symbol}";
        }
    }
}