namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    /// <summary>
    /// The pair of horizontal metrics for an individual glyph.
    /// </summary>
    public struct HorizontalMetric
    {
        /// <summary>
        /// The advance width.
        /// </summary>
        public ushort AdvanceWidth { get; }

        /// <summary>
        /// The left side bearing.
        /// </summary>
        public short LeftSideBearing { get; }

        /// <summary>
        /// Create a new <see cref="HorizontalMetric"/>.
        /// </summary>
        public HorizontalMetric(ushort advanceWidth, short leftSideBearing)
        {
            AdvanceWidth = advanceWidth;
            LeftSideBearing = leftSideBearing;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Width: {AdvanceWidth}. LSB: {LeftSideBearing}.";
        }
    }
}
