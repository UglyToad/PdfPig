namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using Tables;

    internal class HorizontalMetricsTableParser : ITrueTypeTableParser<HorizontalMetricsTable>
    {
        public HorizontalMetricsTable Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register)
        {
            var glyphCount = register.MaximumProfileTable.NumberOfGlyphs;
            var metricCount = register.HorizontalHeaderTable.NumberOfHeaderMetrics;

            data.Seek(header.Offset);

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

            return new HorizontalMetricsTable(header, advancedWidths, leftSideBearings, metricCount);
        }
    }
}
