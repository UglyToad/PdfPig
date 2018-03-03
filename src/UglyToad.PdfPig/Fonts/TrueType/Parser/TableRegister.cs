namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System;
    using System.Collections.Generic;
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
    }
}
