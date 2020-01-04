namespace UglyToad.PdfPig.Fonts.TrueType.Tables.Kerning
{
    /// <summary>
    /// A kerning value for a pair of glyphs.
    /// </summary>
    public struct KernPair
    {
        /// <summary>
        /// The index of the left-hand glyph.
        /// </summary>
        public int LeftGlyphIndex { get; }

        /// <summary>
        /// The index of the right-hand glyph.
        /// </summary>
        public int RightGlyphIndex { get; }

        /// <summary>
        /// The kerning value. For values greater than zero the characters are moved apart.
        /// For values less than zero the characters are moved closer together.
        /// </summary>
        public short Value { get; }

        /// <summary>
        /// Create a new <see cref="KernPair"/>.
        /// </summary>
        public KernPair(int leftGlyphIndex, int rightGlyphIndex, short value)
        {
            LeftGlyphIndex = leftGlyphIndex;
            RightGlyphIndex = rightGlyphIndex;
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Left: {LeftGlyphIndex}, Right: {RightGlyphIndex}, Value {Value}.";
        }
    }
}
