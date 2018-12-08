namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    /// <inheritdoc />
    /// <summary>
    /// The format 0 sub-total where character codes and glyph indices are restricted to a single bytes.
    /// </summary>
    internal class ByteEncodingCMapTable : ICMapSubTable
    {
        private const int GlyphMappingLength = 256;
        private readonly byte[] glyphMapping;

        public TrueTypeCMapPlatform PlatformId { get; }

        public int EncodingId { get; }

        public int FirstCharacterCode { get; }

        public int LastCharacterCode { get; }

        private ByteEncodingCMapTable(TrueTypeCMapPlatform platformId, int encodingId, byte[] glyphMapping)
        {
            this.glyphMapping = glyphMapping;
            PlatformId = platformId;
            EncodingId = encodingId;
        }

        public static ByteEncodingCMapTable Load(TrueTypeDataBytes data, TrueTypeCMapPlatform platformId, int encodingId)
        {
            // ReSharper disable UnusedVariable
            var length = data.ReadUnsignedShort();
            var version = data.ReadUnsignedShort();
            // ReSharper restore UnusedVariable

            var glyphMapping = data.ReadByteArray(GlyphMappingLength);

            return new ByteEncodingCMapTable(platformId, encodingId, glyphMapping);
        }

        public int CharacterCodeToGlyphIndex(int characterCode)
        {
            if (characterCode < 0 || characterCode >= GlyphMappingLength)
            {
                return 0;
            }

            return glyphMapping[characterCode];
        }
    }
}