namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using Glyphs;
    using Tables;

    internal class HorizontalMetricsTableParser : ITrueTypeTableParser<HorizontalMetricsTable>
    {
        public HorizontalMetricsTable Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register)
        {
            var glyphCount = register.MaximumProfileTable.NumberOfGlyphs;
            var metricCount = register.HorizontalHeaderTable.NumberOfHeaderMetrics;

            data.Seek(header.Offset);
            var bytesRead = 0;


            var horizontalMetrics = new HorizontalMetric[metricCount];


            for (var i = 0; i < metricCount; i++)
            {
                var width = data.ReadUnsignedShort();
                var lsb = data.ReadSignedShort();

                horizontalMetrics[i] = new HorizontalMetric(width, lsb);

                bytesRead += 4;
            }
            
            // The number of entries in the left side bearing field per entry is number of glyphs - number of metrics
            // For bearings over the metric count, the width is the same as the last width in advanced widths.
            var additionalLeftSideBearings = new short[glyphCount - metricCount];

            for (var i = 0; i < additionalLeftSideBearings.Length; i++)
            {
                if (bytesRead >= header.Length)
                {
                    break;
                }

                additionalLeftSideBearings[i] = data.ReadSignedShort();
                bytesRead += 2;
            }

            return new HorizontalMetricsTable(header, horizontalMetrics, additionalLeftSideBearings);
        }
    }
}
