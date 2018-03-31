namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using Parser;
    using System.Collections.Generic;
    using CMapSubTables;

    internal class CMapTable : ITable
    {
        private readonly IReadOnlyList<ICMapSubTable> subTables;

        public string Tag => TrueTypeHeaderTable.Cmap;

        public int Version { get; }

        public TrueTypeHeaderTable DirectoryTable { get; }

        public CMapTable(int version, TrueTypeHeaderTable directoryTable, IReadOnlyList<ICMapSubTable> subTables)
        {
            this.subTables = subTables;
            Version = version;
            DirectoryTable = directoryTable;
        }

        public static CMapTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, TableRegister tableRegister)
        {
            data.Seek(table.Offset);

            var tableVersionNumber = data.ReadUnsignedShort();

            var numberOfEncodingTables = data.ReadUnsignedShort();

            var subTableHeaders = new SubTableHeaderEntry[numberOfEncodingTables];

            for (int i = 0; i < numberOfEncodingTables; i++)
            {
                var platformId = data.ReadUnsignedShort();
                var encodingId = data.ReadUnsignedShort();
                var offset = data.ReadUnsignedInt();

                subTableHeaders[i] = new SubTableHeaderEntry(platformId, encodingId, offset);
            }

            var tables = new List<ICMapSubTable>(numberOfEncodingTables);

            var numberofGlyphs = tableRegister.MaximumProfileTable.NumberOfGlyphs;

            for (var i = 0; i < subTableHeaders.Length; i++)
            {
                var header = subTableHeaders[i];

                data.Seek(table.Offset + header.Offset);

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
                            var item = ByteEncodingCMapTable.Load(data, header.PlatformId, header.EncodingId);
                            tables.Add(item);
                            break;
                        }
                    case 2:
                        {
                            // Useful for CJK characters. Use mixed 8/16 bit encoding.
                            var item = HighByteMappingCMapTable.Load(data, numberofGlyphs, header.PlatformId, header.EncodingId);
                            tables.Add(item);
                            break;
                        }
                    case 4:
                        {
                            // Microsoft's standard mapping table.
                            var item = Format4CMapTable.Load(data, header.PlatformId, header.EncodingId);
                            tables.Add(item);
                            break;
                        }
                    case 6:
                        {
                            var item = TrimmedTableMappingCMapTable.Load(data, header.PlatformId, header.EncodingId);
                            tables.Add(item);
                            break;
                        }
                }
            }

            return new CMapTable(tableVersionNumber, table, tables);
        }

        private class SubTableHeaderEntry
        {
            public int PlatformId { get; }

            public int EncodingId { get; }

            public long Offset { get; }

            public SubTableHeaderEntry(int platformId, int encodingId, long offset)
            {
                PlatformId = platformId;
                EncodingId = encodingId;
                Offset = offset;
            }
        }
    }
}
