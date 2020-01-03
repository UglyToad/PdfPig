namespace UglyToad.PdfPig.Writer.Fonts.Subsetting
{
    using System;
    using PdfPig.Fonts.TrueType.Tables;

    /// <summary>
    /// Details of the new glyph 'glyf' table created when subsetting a TrueType font.
    /// </summary>
    internal class TrueTypeSubsetGlyphTable
    {
        /// <summary>
        /// The raw bytes of the new table.
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// The offsets of each of the glyphs in the new table.
        /// </summary>
        public uint[] GlyphOffsets { get; }

        /// <summary>
        /// The corresponding horizontal metrics for each glyph.
        /// </summary>
        public HorizontalMetricsTable.HorizontalMetric[] HorizontalMetrics { get; }

        /// <summary>
        /// The number of glyphs in the new table.
        /// </summary>
        public ushort GlyphCount => (ushort)(GlyphOffsets.Length - 1);

        /// <summary>
        /// Create a new <see cref="TrueTypeSubsetGlyphTable"/>.
        /// </summary>
        public TrueTypeSubsetGlyphTable(byte[] bytes, uint[] glyphOffsets, HorizontalMetricsTable.HorizontalMetric[] horizontalMetrics)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            GlyphOffsets = glyphOffsets ?? throw new ArgumentNullException(nameof(glyphOffsets));
            HorizontalMetrics = horizontalMetrics ?? throw new ArgumentNullException(nameof(horizontalMetrics));
        }

        /// <summary>
        /// Convert the <see cref="GlyphOffsets"/> values to <see langword="long"/>s.
        /// </summary>
        public long[] OffsetsAsLongs()
        {
            var data = new long[GlyphOffsets.Length];
            for (var i = 0; i < GlyphOffsets.Length; i++)
            {
                data[i] = GlyphOffsets[i];
            }

            return data;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{GlyphCount} glyphs. Data is {Bytes.Length} bytes.";
        }
    }
}