namespace UglyToad.Pdf.Fonts.TrueType.Parser
{
    using Tables;

    internal class HeaderTableParser
    {
        public string Tag => TrueTypeFontTable.Head;

        public HeaderTable Parse(TrueTypeDataBytes data, TrueTypeFontTable table)
        {
            data.Seek(table.Offset - 1);
            var version = data.Read32Fixed();
            var fontRevision = data.Read32Fixed();
            var checkSumAdjustment = data.ReadUnsignedInt();
            var magicNumber = data.ReadUnsignedInt();
            var flags = data.ReadUnsignedShort();
            var unitsPerEm = data.ReadUnsignedShort();
            var created = data.ReadInternationalDate();
            var modified = data.ReadInternationalDate();
            var xMin = data.ReadSignedShort();
            var yMin = data.ReadSignedShort();
            var xMax = data.ReadSignedShort();
            var yMax = data.ReadSignedShort();
            var macStyle = data.ReadUnsignedShort();
            var lowestRecPpem = data.ReadUnsignedShort();
            var fontDirectionHint = data.ReadSignedShort();
            var indexToLocFormat = data.ReadSignedShort();
            var glyphDataFormat = data.ReadSignedShort();

            return new HeaderTable(table, (decimal)version, (decimal)fontRevision, checkSumAdjustment,
                magicNumber, flags, unitsPerEm, created, modified,
                xMin, yMin, xMax, yMax, macStyle, lowestRecPpem,
                fontDirectionHint, indexToLocFormat, glyphDataFormat);
        }
    }
}
