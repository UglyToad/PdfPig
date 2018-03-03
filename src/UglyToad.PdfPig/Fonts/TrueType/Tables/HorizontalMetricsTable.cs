namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using Parser;

    internal class HorizontalMetricsTable : ITable
    {
        private readonly int[] advancedWidths;
        private readonly short[] leftSideBearings;

        public string Tag => TrueTypeHeaderTable.Hmtx;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public HorizontalMetricsTable(TrueTypeHeaderTable directoryTable, int[] advancedWidths, short[] leftSideBearings)
        {
            this.advancedWidths = advancedWidths;
            this.leftSideBearings = leftSideBearings;
            DirectoryTable = directoryTable;
        }

        public static HorizontalMetricsTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, TableRegister tableRegister)
        {
            var metricCount = tableRegister.HorizontalHeaderTable.NumberOfHeaderMetrics;
            var glyphCount = tableRegister.MaximumProfileTable.NumberOfGlyphs;

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

            return new HorizontalMetricsTable(table, advancedWidths, leftSideBearings);
        }
    }
}
