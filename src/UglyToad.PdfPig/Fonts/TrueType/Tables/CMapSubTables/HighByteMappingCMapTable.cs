namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    using System;
    using System.Collections.Generic;

    /// <inheritdoc />
    /// <summary>
    /// A format 2 sub-table for Chinese, Japanese and Korean characters.
    /// Contains mixed 8/16 bit encodings.
    /// </summary>
    internal class HighByteMappingCMapTable : ICMapSubTable
    {
        private readonly IReadOnlyDictionary<int, int> characterCodesToGlyphIndices;

        public int PlatformId { get; }

        public int EncodingId { get; }

        private HighByteMappingCMapTable(int platformId, int encodingId, IReadOnlyDictionary<int, int> characterCodesToGlyphIndices)
        {
            this.characterCodesToGlyphIndices = characterCodesToGlyphIndices ?? throw new ArgumentNullException(nameof(characterCodesToGlyphIndices));
            PlatformId = platformId;
            EncodingId = encodingId;
        }

        public int CharacterCodeToGlyphIndex(int characterCode)
        {
            if (!characterCodesToGlyphIndices.TryGetValue(characterCode, out var index))
            {
                return 0;
            }

            return index;
        }

        public static HighByteMappingCMapTable Load(TrueTypeDataBytes data, int numberOfGlyphs, int platformId, int encodingId)
        {
            // ReSharper disable UnusedVariable
            var length = data.ReadUnsignedShort();
            var version = data.ReadUnsignedShort();
            // ReSharper restore UnusedVariable

            var subHeaderKeys = new int[256];
            var maximumSubHeaderIndex = 0;
            for (var i = 0; i < 256; i++)
            {
                var value = data.ReadUnsignedShort();
                maximumSubHeaderIndex = Math.Max(maximumSubHeaderIndex, value / 8);
                subHeaderKeys[i] = value;

            }

            var subHeaderCount = maximumSubHeaderIndex + 1;

            var subHeaders = new SubHeader[subHeaderCount];

            for (var i = 0; i < subHeaderCount; i++)
            {
                var firstCode = data.ReadUnsignedShort();
                var entryCount = data.ReadUnsignedShort();
                var idDelta = data.ReadSignedShort();
                var idRangeOffset = data.ReadUnsignedShort() - (subHeaderCount - i - 1) * 8 - 2;
                subHeaders[i] = new SubHeader(firstCode, entryCount, idDelta, idRangeOffset);
            }

            var glyphIndexArrayOffset = data.Position;

            var characterCodeToGlyphId = new Dictionary<int, int>();

            for (var i = 0; i < subHeaderCount; i++)
            {
                var subHeader = subHeaders[i];

                data.Seek(glyphIndexArrayOffset + subHeader.IdRangeOffset);

                for (int j = 0; j < subHeader.EntryCount; j++)
                {
                    int characterCode = (i << 8) + (subHeader.FirstCode + j);

                    var p = (int)data.ReadUnsignedShort();

                    if (p > 0)
                    {
                        p = (p + subHeader.IdDelta) % 65536;
                    }

                    if (p >= numberOfGlyphs)
                    {
                        continue;
                    }

                    characterCodeToGlyphId[characterCode] = p;
                }
            }

            return new HighByteMappingCMapTable(platformId, encodingId, characterCodeToGlyphId);
        }

        public struct SubHeader
        {
            /// <summary>
            /// First valid low byte for the sub header.
            /// </summary>
            public int FirstCode { get; }

            /// <summary>
            /// Number of valid low bytes for the sub header.
            /// </summary>
            public int EntryCount { get; }

            /// <summary>
            /// Adds to the value from the sub array to provide the glyph index.
            /// </summary>
            public short IdDelta { get; }

            /// <summary>
            /// The number of bytes past the actual location of this value where the glyph index array element starts.
            /// </summary>
            public int IdRangeOffset { get; }

            public SubHeader(int firstCode, int entryCount, short idDelta, int idRangeOffset)
            {
                FirstCode = firstCode;
                EntryCount = entryCount;
                IdDelta = idDelta;
                IdRangeOffset = idRangeOffset;
            }
        }
    }
}