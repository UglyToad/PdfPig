namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using Tables;

    /// <summary>
    /// Holds tables while parsing a TrueType font
    /// </summary>
    internal class TableRegister
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
        /// Can contain mutliple sub-tables to support multiple encoding schemes.
        /// Where a character code isn't found it should map to index 0.
        /// </summary>
        public CMapTable CMapTable { get; set; }
    }
}
