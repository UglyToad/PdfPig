namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System;
    using Tables;

    internal class HorizontalHeaderTableParser : ITrueTypeTableParser<HorizontalHeaderTable>
    {
        public HorizontalHeaderTable Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register)
        {
            data.Seek(header.Offset);
            var majorVersion = data.ReadUnsignedShort();
            var minorVersion = data.ReadUnsignedShort();

            var ascender = data.ReadSignedShort();
            var descender = data.ReadSignedShort();
            var lineGap = data.ReadSignedShort();

            var advancedWidthMax = data.ReadUnsignedShort();

            var minLeftSideBearing = data.ReadSignedShort();
            var minRightSideBearing = data.ReadSignedShort();
            var xMaxExtent = data.ReadSignedShort();

            var caretSlopeRise = data.ReadSignedShort();
            var caretSlopeRun = data.ReadSignedShort();
            var caretOffset = data.ReadSignedShort();

            // Reserved section
            data.ReadSignedShort();
            data.ReadSignedShort();
            data.ReadSignedShort();
            data.ReadSignedShort();

            var metricDataFormat = data.ReadSignedShort();

            if (metricDataFormat != 0)
            {
                throw new NotSupportedException("The metric data format for a horizontal header table should be 0.");
            }

            var numberOfHeaderMetrics = data.ReadUnsignedShort();

            return new HorizontalHeaderTable(header, majorVersion, minorVersion, ascender,
                descender, lineGap, advancedWidthMax,
                minLeftSideBearing,
                minRightSideBearing,
                xMaxExtent,
                caretSlopeRise,
                caretSlopeRun,
                caretOffset,
                metricDataFormat,
                numberOfHeaderMetrics);
        }
    }
}