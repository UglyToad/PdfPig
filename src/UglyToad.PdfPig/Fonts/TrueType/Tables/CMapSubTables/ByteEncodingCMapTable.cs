namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    internal class ByteEncodingCMapTable : ICMapSubTable
    {
        public static ByteEncodingCMapTable Load(TrueTypeDataBytes data)
        {
            var length = data.ReadUnsignedShort();
            var version = data.ReadUnsignedShort();

            var glyphMapping = data.ReadByteArray(256);

            return new ByteEncodingCMapTable();
        }
    }
}