// ReSharper disable UnusedVariable
namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    using System;
    using System.IO;
    using System.Linq;
    using Core;

    /// <summary>
    /// A format 6 CMap sub-table which uses 2 bytes to map a contiguous range of character codes to glyph indices.
    /// </summary>
    internal class TrimmedTableMappingCMapTable : ICMapSubTable, IWriteable
    {
        private const ushort Format = 6;
        private const ushort DefaultLanguageId = 0;

        private readonly int entryCount;
        private readonly ushort[] glyphIndices;

        /// <inheritdoc />
        public TrueTypeCMapPlatform PlatformId { get; }

        /// <inheritdoc />
        public ushort EncodingId { get; }

        public int FirstCharacterCode { get; }

        public int LastCharacterCode { get; }

        /// <summary>
        /// Create a new <see cref="TrimmedTableMappingCMapTable"/>.
        /// </summary>
        public TrimmedTableMappingCMapTable(TrueTypeCMapPlatform platformId, ushort encodingId, int firstCharacterCode, int entryCount, ushort[] glyphIndices)
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

        public int[] GetCharactersCode()
        {
            return Enumerable.Range(0, entryCount).Where(i => glyphIndices[i] != 0).Select(i => FirstCharacterCode + i).ToArray();
        }

        public static TrimmedTableMappingCMapTable Load(TrueTypeDataBytes data, TrueTypeCMapPlatform platformId, ushort encodingId)
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

        public void Write(Stream stream)
        {
            stream.WriteUShort(Format);

            var length = (ushort)((5 * sizeof(short)) + (sizeof(short) * glyphIndices.Length));

            stream.WriteUShort(length);
            stream.WriteUShort(DefaultLanguageId);
            stream.WriteUShort(FirstCharacterCode);
            stream.WriteUShort(glyphIndices.Length);

            for (var j = 0; j < glyphIndices.Length; j++)
            {
                stream.WriteUShort(glyphIndices[j]);
            }
        }
    }
}
