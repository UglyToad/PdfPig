namespace UglyToad.Pdf.Fonts.TrueType.Parser
{
    using System;
    using System.Collections.Generic;
    using Util.JetBrains.Annotations;

    internal class TrueTypeFontParser
    {
        private const int TagLength = 4;

        private static readonly IReadOnlyDictionary<string, ITrueTypeTableParser> parsers =
            new Dictionary<string, ITrueTypeTableParser>
            {
                {TrueTypeFontTable.Head, new HeaderTableParser()}
            };

        public void Parse(TrueTypeDataBytes data)
        {
            var version = data.Read32Fixed();
            int numberOfTables = data.ReadUnsignedShort();
            int searchRange = data.ReadUnsignedShort();
            int entrySelector = data.ReadUnsignedShort();
            int rangeShift = data.ReadUnsignedShort();

            var tables = new Dictionary<string, TrueTypeFontTable>();

            for (var i = 0; i < numberOfTables; i++)
            {
                var table = ReadTable(data);

                if (table != null)
                {
                    tables[table.Tag] = table;
                }
            }

            ParseTables(tables, data);

            return;
        }

        [CanBeNull]
        private static TrueTypeFontTable ReadTable(TrueTypeDataBytes data)
        {
            var tag = data.ReadString(TagLength);
            var checksum = data.ReadUnsignedInt();
            var offset = data.ReadUnsignedInt();
            var length = data.ReadUnsignedInt();

            // skip tables with zero length (except glyf)
            if (length == 0 && !string.Equals(tag, TrueTypeFontTable.Glyf))
            {
                return null;
            }

            return new TrueTypeFontTable(tag, checksum, offset, length);
        }

        private static void ParseTables(IReadOnlyDictionary<string, TrueTypeFontTable> tables, TrueTypeDataBytes data)
        {
            var isPostScript = tables.ContainsKey(TrueTypeFontTable.Cff);

            if (!tables.TryGetValue(TrueTypeFontTable.Head, out var table))
            {
                throw new InvalidOperationException($"The {TrueTypeFontTable.Head} table is required.");
            }

            var header = parsers[TrueTypeFontTable.Head].Parse(data, table);
        }
    }
}
