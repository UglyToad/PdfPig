namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    using System;
    using System.Collections.Generic;

    internal class HighByteMappingCMapTable : ICMapSubTable
    {
        public static HighByteMappingCMapTable Load(TrueTypeDataBytes data, int numberOfGlyphs)
        {
            var length = data.ReadUnsignedShort();
            var version = data.ReadUnsignedShort();

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

                    var p = data.ReadUnsignedShort();

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

            return new HighByteMappingCMapTable();
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