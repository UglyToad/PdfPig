namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System;
    using Tables;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Holds tables while contained in a TrueType font.
    /// </summary>
    internal class TableRegister
    {
        /// <summary>
        /// This table contains global information about the font. 
        /// </summary>
        [NotNull]
        public HeaderTable HeaderTable { get; }

        /// <summary>
        /// This table contains the data that defines the appearance of the glyphs in the font.
        /// </summary>
        [NotNull]
        public GlyphDataTable GlyphTable { get; }

        /// <summary>
        /// This table contains information needed to layout fonts whose characters are written horizontally.
        /// </summary>
        [NotNull]
        public HorizontalHeaderTable HorizontalHeaderTable { get; }

        /// <summary>
        /// This table contains metric information for the horizontal layout each of the glyphs in the font.
        /// </summary>
        [NotNull]
        public HorizontalMetricsTable HorizontalMetricsTable { get; }

        /// <summary>
        /// This table stores the offsets to the locations of the glyphs (relative to the glyph table).
        /// </summary>
        [NotNull]
        public IndexToLocationTable IndexToLocationTable { get; }

        /// <summary>
        /// This table establishes the memory requirements for the font.
        /// </summary>
        [NotNull]
        public BasicMaximumProfileTable MaximumProfileTable { get; }

        [CanBeNull]
        public NameTable NameTable { get; }

        public PostScriptTable PostScriptTable { get; }

        /// <summary>
        /// Defines mapping of character codes to glyph index values in the font.
        /// Can contain multiple sub-tables to support multiple encoding schemes.
        /// Where a character code isn't found it should map to index 0.
        /// </summary>
        public CMapTable CMapTable { get; }

        public KerningTable KerningTable { get; }

        /// <summary>
        /// Create a new <see cref="TableRegister"/>.
        /// </summary>
        /// <param name="builder">The builder with necessary tables set.</param>
        public TableRegister([NotNull] Builder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            HeaderTable = builder.HeaderTable ?? throw new ArgumentException("The builder did not contain the header table");
            GlyphTable = builder.GlyphDataTable ?? throw new ArgumentException("The builder did not contain the glyph data table.");
            HorizontalHeaderTable = builder.HorizontalHeaderTable ?? throw new ArgumentException("The builder did not contain the horizontal header table.");
            HorizontalMetricsTable = builder.HorizontalMetricsTable;
            IndexToLocationTable = builder.IndexToLocationTable ?? throw new ArgumentException("The builder did not contain the index to location table.");
            MaximumProfileTable = builder.MaximumProfileTable ?? throw new ArgumentException("The builder did not contain the maximum profile table.");
            NameTable = builder.NameTable;
            PostScriptTable = builder.PostScriptTable;
            CMapTable = builder.CMapTable;
            KerningTable = builder.KerningTable;
        }

        /// <summary>
        /// Used to gather the necessary tables for a TrueType font.
        /// </summary>
        public class Builder
        {
            public GlyphDataTable GlyphDataTable { get; set; }

            public HeaderTable HeaderTable { get; set; }

            public HorizontalHeaderTable HorizontalHeaderTable { get; set; }

            public HorizontalMetricsTable HorizontalMetricsTable { get; set; }

            public IndexToLocationTable IndexToLocationTable { get; set; }

            public BasicMaximumProfileTable MaximumProfileTable { get; set; }

            public PostScriptTable PostScriptTable { get; set; }

            /// <summary>
            /// Defines mapping of character codes to glyph index values in the font.
            /// Can contain multiple sub-tables to support multiple encoding schemes.
            /// Where a character code isn't found it should map to index 0.
            /// </summary>
            public CMapTable CMapTable { get; set; }

            public KerningTable KerningTable { get; set; }

            public NameTable NameTable { get; set; }

            public TableRegister Build()
            {
                return new TableRegister(this);
            }
        }
    }
}
