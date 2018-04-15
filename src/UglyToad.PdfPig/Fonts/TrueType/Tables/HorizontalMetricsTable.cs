namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using Parser;

    internal class HorizontalMetricsTable : ITable
    {
        private readonly int[] advancedWidths;
        private readonly short[] leftSideBearings;

        private readonly int metricCount;

        public string Tag => TrueTypeHeaderTable.Hmtx;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public HorizontalMetricsTable(TrueTypeHeaderTable directoryTable, int[] advancedWidths, short[] leftSideBearings, int metricCount)
        {
            this.advancedWidths = advancedWidths;
            this.leftSideBearings = leftSideBearings;
            this.metricCount = metricCount;

            DirectoryTable = directoryTable;
        }

        public static HorizontalMetricsTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, TableRegister tableRegister)
        {
            var glyphCount = tableRegister.MaximumProfileTable.NumberOfGlyphs;
            var metricCount = tableRegister.HorizontalHeaderTable.NumberOfHeaderMetrics;

            data.Seek(table.Offset);

            // The number of entries in the left side bearing field per entry is number of glyphs - number of metrics
            var additionalLeftSideBearingLength = glyphCount - metricCount;

            var advancedWidths = new int[metricCount];

            // For bearings over the metric count, the width is the same as the last width in advanced widths.
            var leftSideBearings = new short[glyphCount];

            for (var i = 0; i < metricCount; i++)
            {
                advancedWidths[i] = data.ReadUnsignedShort();
                leftSideBearings[i] = data.ReadSignedShort();
            }

            for (var i = 0; i < additionalLeftSideBearingLength; i++)
            {
                leftSideBearings[metricCount + i] = data.ReadSignedShort();
            }

            return new HorizontalMetricsTable(table, advancedWidths, leftSideBearings, metricCount);
        }

        public int GetAdvanceWidth(int index)
        {
            if (index < metricCount)
            {
                return advancedWidths[index];
            }

            // monospaced fonts may not have a width for every glyph
            // the last one is for subsequent glyphs
            return advancedWidths[advancedWidths.Length - 1];
        }
    }
}
