// ReSharper disable UnusedVariable
namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    using System;

    /// <inheritdoc />
    /// <summary>
    /// A format 6 CMap sub-table which uses 2 bytes to map a contiguous range of character codes to glyph indices.
    /// </summary>
    internal class TrimmedTableMappingCMapTable : ICMapSubTable
    {
        private readonly int entryCount;
        private readonly int[] glyphIndices;

        public TrueTypeCMapPlatform PlatformId { get; }
        public int EncodingId { get; }

        public int FirstCharacterCode { get; }

        public int LastCharacterCode { get; }

        /// <summary>
        /// Create a new <see cref="TrimmedTableMappingCMapTable"/>.
        /// </summary>
        public TrimmedTableMappingCMapTable(TrueTypeCMapPlatform platformId, int encodingId, int firstCharacterCode, int entryCount, int[] glyphIndices)
        {
            FirstCharacterCode = firstCharacterCode;
            this.entryCount = entryCount;
            this.glyphIndices = glyphIndices ?? throw new ArgumentNullException(nameof(glyphIndices));

            LastCharacterCode = firstCharacterCode + entryCount - 1;

            PlatformId = platformId;
            EncodingId = encodingId;
        }

        public int CharacterCodeToGlyphIndex(int characterCode)
        {
            if (characterCode < FirstCharacterCode || characterCode > FirstCharacterCode + entryCount)
            {
                return 0;
            }

            var offset = characterCode - FirstCharacterCode;

            if (offset < 0 || offset >= glyphIndices.Length)
            {
                return 0;
            }

            return glyphIndices[offset];
        }

        public static TrimmedTableMappingCMapTable Load(TrueTypeDataBytes data, TrueTypeCMapPlatform platformId, int encodingId)
        {
            var length = data.ReadUnsignedShort();
            var language = data.ReadUnsignedShort();

            // First character code in the range.
            var firstCode = data.ReadUnsignedShort();

            // Number of character codes in the range.
            var entryCount = data.ReadUnsignedShort();
            
            var glyphIndices = data.ReadUnsignedShortArray(entryCount);

            return new TrimmedTableMappingCMapTable(platformId, encodingId, firstCode, entryCount, glyphIndices);
        }
    }
}
