namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System.Collections.Generic;
    using Tables;
    using Tables.CMapSubTables;

    internal class CMapTableParser : ITrueTypeTableParser<CMapTable>
    {
        public CMapTable Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register)
        {
            data.Seek(header.Offset);

            var tableVersionNumber = data.ReadUnsignedShort();

            var numberOfEncodingTables = data.ReadUnsignedShort();

            var subTableHeaders = new SubTableHeaderEntry[numberOfEncodingTables];

            for (var i = 0; i < numberOfEncodingTables; i++)
            {
                var platformId = (TrueTypeCMapPlatform)data.ReadUnsignedShort();
                var encodingId = data.ReadUnsignedShort();
                var offset = data.ReadUnsignedInt();

                subTableHeaders[i] = new SubTableHeaderEntry(platformId, encodingId, offset);
            }

            var tables = new List<ICMapSubTable>(numberOfEncodingTables);

            var numberofGlyphs = register.MaximumProfileTable.NumberOfGlyphs;

            for (var i = 0; i < subTableHeaders.Length; i++)
            {
                var subTableHeader = subTableHeaders[i];

                data.Seek(header.Offset + subTableHeader.Offset);

                var format = data.ReadUnsignedShort();

                /*
                 * There are 9 currently available formats:
                 * 0: Character code and glyph indices are restricted to a single byte. Rare.
                 * 2: Suitable for CJK characters. Contain mixed 8/16 byte encoding.
                 * 4: 2 byte encoding format. Used when character codes fall into (gappy) contiguous ranges.
                 * 6: 'Trimmed table mapping', used when character codes fall into a single contiguous range. This is dense mapping.
                 * 8: 16/32 bit coverage. Uses mixed length character codes.
                 * 10: Similar to format 6, trimmed table/array for 32 bits.
                 * 12: Segmented coverage, similar to format 4 but for 32 bit/4 byte.
                 * 13: Many to one mappings. Used by Apple for the LastResort font.
                 * 14: Unicode variation sequences.
                 *
                 * Many of the formats are obsolete or not really used. Modern fonts will tend to use formats 4, 6 and 12.
                 * For PDF we will support 0, 2 and 4 since these are in the original TrueType spec.
                 */
                switch (format)
                {
                    case 0:
                        {
                            // Simple 1 to 1 mapping of character codes to glyph codes.
                            var item = ByteEncodingCMapTable.Load(data, subTableHeader.PlatformId, subTableHeader.EncodingId);
                            tables.Add(item);
                            break;
                        }
                    case 2:
                        {
                            // Useful for CJK characters. Use mixed 8/16 bit encoding.
                            var item = HighByteMappingCMapTable.Load(data, numberofGlyphs, subTableHeader.PlatformId, subTableHeader.EncodingId);
                            tables.Add(item);
                            break;
                        }
                    case 4:
                        {
                            // Microsoft's standard mapping table.
                            var item = Format4CMapTable.Load(data, subTableHeader.PlatformId, subTableHeader.EncodingId);
                            tables.Add(item);
                            break;
                        }
                    case 6:
                        {
                            var item = TrimmedTableMappingCMapTable.Load(data, subTableHeader.PlatformId, subTableHeader.EncodingId);
                            tables.Add(item);
                            break;
                        }
                }
            }

            return new CMapTable(tableVersionNumber, header, tables);
        }

        private class SubTableHeaderEntry
        {
            public TrueTypeCMapPlatform PlatformId { get; }

            public ushort EncodingId { get; }

            public long Offset { get; }

            public SubTableHeaderEntry(TrueTypeCMapPlatform platformId, ushort encodingId, long offset)
            {
                PlatformId = platformId;
                EncodingId = encodingId;
                Offset = offset;
            }
        }
    }
}
