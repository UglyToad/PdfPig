namespace UglyToad.PdfPig.Fonts.TrueType.Subsetting
{
    using System;
    using Glyphs;

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
        public HorizontalMetric[] HorizontalMetrics { get; }

        /// <summary>
        /// The number of glyphs in the new table.
        /// </summary>
        public ushort GlyphCount => (ushort)(GlyphOffsets.Length - 1);

        /// <summary>
        /// Create a new <see cref="TrueTypeSubsetGlyphTable"/>.
        /// </summary>
        public TrueTypeSubsetGlyphTable(byte[] bytes, uint[] glyphOffsets, HorizontalMetric[] horizontalMetrics)
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