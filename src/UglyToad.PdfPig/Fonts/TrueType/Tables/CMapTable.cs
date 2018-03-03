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

                switch (format)
                {
                    case 0:
                        {
                            // Simple 1 to 1 mapping of character codes to glyph codes.
                            var item = ByteEncodingCMapTable.Load(data);
                            tables.Add(item);
                            break;
                        }
                    case 1:
                        {
                            // Useful for CJK characters. Use mixed 8/16 bit encoding.
                            var item = HighByteMappingCMapTable.Load(data, numberofGlyphs);
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
