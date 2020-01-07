namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    using System.IO;
    using Core;

    /// <summary>
    /// The format 0 sub-table where character codes and glyph indices are restricted to a single bytes.
    /// </summary>
    internal class ByteEncodingCMapTable : ICMapSubTable, IWriteable
    {
        private const ushort Format = 0;
        private const ushort DefaultLanguageId = 0;
        private const int SizeOfShort = 2;
        private const int GlyphMappingLength = 256;

        private readonly byte[] glyphMapping;

        public TrueTypeCMapPlatform PlatformId { get; }

        public ushort EncodingId { get; }

        public ushort LanguageId { get; }

        public ByteEncodingCMapTable(TrueTypeCMapPlatform platformId, ushort encodingId, ushort languageId, byte[] glyphMapping)
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

        public void Write(Stream stream)
        {
            stream.WriteUShort(Format);
            stream.WriteUShort(GlyphMappingLength + (SizeOfShort * 3));
            stream.WriteUShort(DefaultLanguageId);
            
            for (var i = 0; i < glyphMapping.Length; i++)
            {
                stream.WriteByte(glyphMapping[i]);
            }
        }
    }
}