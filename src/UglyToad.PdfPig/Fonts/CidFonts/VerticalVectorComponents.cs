namespace UglyToad.PdfPig.Fonts.CidFonts
{
    using Geometry;

    /// <summary>
    /// Defines the default position and displacement vector vertical components
    /// for fonts which have vertical writing modes.
    /// </summary>
    internal struct VerticalVectorComponents
    {
        /// <summary>
        /// The default value of <see cref="VerticalVectorComponents"/> if not defined by a font.
        /// </summary>
        public static readonly VerticalVectorComponents Default = new VerticalVectorComponents(800, -1000);

        /// <summary>
        /// The vertical component of the position vector.
        /// </summary>
        /// <remarks>
        /// The full position vector unless overridden by the W2 array is:
        /// (w0/2, Position)
        /// Where w0 is the width of the given glyph.
        /// </remarks>
        public decimal Position { get; }
        
        /// <summary>
        /// The vertical component of the displacement vector.
        /// </summary>
        /// <remarks>
        /// The full displacement vector is:
        /// (0, Displacement)
        /// </remarks>
        public decimal Displacement { get; }

        /// <summary>
        /// Create a new <see cref="VerticalVectorComponents"/>.
        /// </summary>
        public VerticalVectorComponents(decimal position, decimal displacement)
        {
            Position = position;
            Displacement = displacement;
        }

        /// <summary>
        /// Get the full position vector for a given glyph.
        /// </summary>
        public PdfVector GetPositionVector(decimal glyphWidth) => new PdfVector(glyphWidth / 2m, Position);

        /// <summary>
        /// Get the full displacement vector.
        /// </summary>
        public PdfVector GetDisplacementVector() => new PdfVector(0, Displacement);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Position: {Position}, Displacement: {Displacement}.";
        }
    }
}