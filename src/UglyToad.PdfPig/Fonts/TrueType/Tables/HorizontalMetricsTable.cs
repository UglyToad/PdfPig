namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System.Collections.Generic;
    using Parser;

    /// <summary>
    /// The 'hmtx' table contains metric information for the horizontal layout each of the glyphs in the font.
    /// </summary>
    internal class HorizontalMetricsTable : ITable
    {
        private readonly short[] leftSideBearings;

        private readonly int metricCount;

        public string Tag => TrueTypeHeaderTable.Hmtx;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public IReadOnlyList<int> AdvancedWidths { get; }

        public HorizontalMetricsTable(TrueTypeHeaderTable directoryTable, int[] advancedWidths, short[] leftSideBearings, int metricCount)
        {
            AdvancedWidths = advancedWidths;
            this.leftSideBearings = leftSideBearings;
            this.metricCount = metricCount;

            DirectoryTable = directoryTable;
        }

        public int GetAdvanceWidth(int index)
        {
            if (index < metricCount)
            {
                return AdvancedWidths[index];
            }

            // monospaced fonts may not have a width for every glyph
            // the last one is for subsequent glyphs
            return AdvancedWidths[AdvancedWidths.Count - 1];
        }
    }
}
