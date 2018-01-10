namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System;
    using System.Collections.Generic;
    using Tables;
    using Util.JetBrains.Annotations;

    internal class TrueTypeFontParser
    {
        public TrueTypeFont Parse(TrueTypeDataBytes data)
        {
            var version = (decimal)data.Read32Fixed();
            int numberOfTables = data.ReadUnsignedShort();
            int searchRange = data.ReadUnsignedShort();
            int entrySelector = data.ReadUnsignedShort();
            int rangeShift = data.ReadUnsignedShort();

            var tables = new Dictionary<string, TrueTypeHeaderTable>();

            for (var i = 0; i < numberOfTables; i++)
            {
                var table = ReadTable(data);

                if (table.HasValue)
                {
                    tables[table.Value.Tag] = table.Value;
                }
            }

            var result = ParseTables(version, tables, data);

            return result;
        }

        [CanBeNull]
        private static TrueTypeHeaderTable? ReadTable(TrueTypeDataBytes data)
        {
            var tag = data.ReadTag();
            var checksum = data.ReadUnsignedInt();
            var offset = data.ReadUnsignedInt();
            var length = data.ReadUnsignedInt();

            // skip tables with zero length (except glyf)
            if (length == 0 && !string.Equals(tag, TrueTypeHeaderTable.Glyf))
            {
                return null;
            }

            return new TrueTypeHeaderTable(tag, checksum, offset, length);
        }

        private static TrueTypeFont ParseTables(decimal version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tables, TrueTypeDataBytes data)
        {
            var isPostScript = tables.ContainsKey(TrueTypeHeaderTable.Cff);

            if (!tables.TryGetValue(TrueTypeHeaderTable.Head, out var table))
            {
                throw new InvalidOperationException($"The {TrueTypeHeaderTable.Head} table is required.");
            }

            var header = HeaderTable.Load(data, table);

            if (!tables.TryGetValue(TrueTypeHeaderTable.Hhea, out var hHead))
            {
                throw new InvalidOperationException("The horizontal header table is required.");
            }

            var horizontalHeader = HorizontalHeaderTable.Load(data, hHead);

            if (!tables.TryGetValue(TrueTypeHeaderTable.Maxp, out var maxHeaderTable))
            {
                throw new InvalidOperationException("The maximum profile table is required.");
            }

            var maximumProfile = BasicMaximumProfileTable.Load(data, maxHeaderTable);

            var postScriptTable = default(PostScriptTable);
            if (tables.TryGetValue(TrueTypeHeaderTable.Post, out var postscriptHeaderTable))
            {
                postScriptTable = PostScriptTable.Load(data, table, maximumProfile);
            }

            if (!isPostScript)
            {
                if (!tables.TryGetValue(TrueTypeHeaderTable.Loca, out var indexToLocationHeaderTable))
                {
                    throw new InvalidOperationException("The location to index table is required for non-PostScript fonts.");
                }

                var indexToLocationTable =
                    IndexToLocationTable.Load(data, indexToLocationHeaderTable, header, maximumProfile);

                if (!tables.TryGetValue(TrueTypeHeaderTable.Glyf, out var glyphHeaderTable))
                {
                    throw new InvalidOperationException("The glpyh table is required for non-PostScript fonts.");
                }

                var glyphTable = GlyphDataTable.Load(data, glyphHeaderTable, header, indexToLocationTable);
            }

            return new TrueTypeFont(version, tables, header);
        }
    }
}
