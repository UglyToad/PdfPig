namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    /// <inheritdoc />
    /// <summary>
    /// The format 0 sub-table where character codes and glyph indices are restricted to a single bytes.
    /// </summary>
    internal class ByteEncodingCMapTable : ICMapSubTable
    {
        private const int SizeOfShort = 2;
        private const int GlyphMappingLength = 256;

        private readonly byte[] glyphMapping;

        public TrueTypeCMapPlatform PlatformId { get; }

        public ushort EncodingId { get; }

        public ushort LanguageId { get; }

        private ByteEncodingCMapTable(TrueTypeCMapPlatform platformId, ushort encodingId, ushort languageId, byte[] glyphMapping)
        {
            this.glyphMapping = glyphMapping;
            PlatformId = platformId;
            EncodingId = encodingId;
            LanguageId = languageId;
        }

        public static ByteEncodingCMapTable Load(TrueTypeDataBytes data, TrueTypeCMapPlatform platformId, ushort encodingId)
        {
            var length = data.ReadUnsignedShort();
            var language = data.ReadUnsignedShort();

            var glyphMapping = data.ReadByteArray(length - (SizeOfShort * 3));

            return new ByteEncodingCMapTable(platformId, encodingId, language, glyphMapping);
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