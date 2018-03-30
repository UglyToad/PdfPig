namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    /// <inheritdoc />
    /// <summary>
    /// The format 0 sub-total where character codes and glyph indices are restricted to a single bytes.
    /// </summary>
    internal class ByteEncodingCMapTable : ICMapSubTable
    {
        public int PlatformId { get; }

        public int EncodingId { get; }

        private ByteEncodingCMapTable(int platformId, int encodingId)
        {
            PlatformId = platformId;
            EncodingId = encodingId;
        }

        public static ByteEncodingCMapTable Load(TrueTypeDataBytes data, int platformId, int encodingId)
        {
            var length = data.ReadUnsignedShort();
            var version = data.ReadUnsignedShort();

            var glyphMapping = data.ReadByteArray(256);

            return new ByteEncodingCMapTable(platformId, encodingId);
        }

        public int CharacterCodeToGlyphIndex(int characterCode)
        {
            throw new System.NotImplementedException();
        }
    }
}