namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Core;
    using Glyphs;

    /// <inheritdoc cref="ITrueTypeTable" />
    /// <summary>
    /// The 'hmtx' table contains metric information for the horizontal layout each of the glyphs in the font.
    /// </summary>
    public class HorizontalMetricsTable : ITrueTypeTable, IWriteable
    {
        /// <inheritdoc />
        public string Tag => TrueTypeHeaderTable.Hmtx;

        /// <inheritdoc />
        public TrueTypeHeaderTable DirectoryTable { get; }

        /// <summary>
        /// The left-side bearing and advance widths for the glyphs in the font. For a monospace font
        /// this may only contain a single entry and the left-side bearings will be defined in <see cref="AdditionalLeftSideBearings"/>.
        /// </summary>
        public IReadOnlyList<HorizontalMetric> HorizontalMetrics { get; }

        /// <summary>
        /// Some fonts may have an array of left side bearings following the <see cref="HorizontalMetrics"/>. 
        /// Generally, this array of left side bearings is used for a run of monospaced glyphs. 
        /// For example, it might be used for a Kanji font or for Courier. 
        /// The corresponding glyphs are assumed to have the same advance width as that found in the last entry in the <see cref="HorizontalMetrics"/>.
        /// </summary>
        public IReadOnlyList<short> AdditionalLeftSideBearings { get; }

        /// <summary>
        /// Create a new <see cref="HorizontalMetricsTable"/>.
        /// </summary>
        public HorizontalMetricsTable(TrueTypeHeaderTable directoryTable, 
            IReadOnlyList<HorizontalMetric> horizontalMetrics,
            IReadOnlyList<short> additionalLeftSideBearings)
        {
            DirectoryTable = directoryTable;
            HorizontalMetrics = horizontalMetrics 
                                ?? throw new ArgumentNullException(nameof(horizontalMetrics));
            AdditionalLeftSideBearings = additionalLeftSideBearings 
                                         ?? throw new ArgumentNullException(nameof(additionalLeftSideBearings));
        }

        /// <summary>
        /// Get the advance width for a glyph at the given index.
        /// </summary>
        public ushort GetAdvanceWidth(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be less than zero.");
            }

            if (index < HorizontalMetrics.Count)
            {
                return HorizontalMetrics[index].AdvanceWidth;
            }

            // Monospaced fonts may not have a width for every glyph, the last metric is for subsequent glyphs.
            return HorizontalMetrics[HorizontalMetrics.Count - 1].AdvanceWidth;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            for (var i = 0; i < HorizontalMetrics.Count; i++)
            {
                var metric = HorizontalMetrics[i];
                stream.WriteUShort(metric.AdvanceWidth);
                stream.WriteShort(metric.LeftSideBearing);
            }

            for (var i = 0; i < AdditionalLeftSideBearings.Count; i++)
            {
                var lsb = AdditionalLeftSideBearings[i];
                stream.WriteShort(lsb);
            }
        }
    }
}
