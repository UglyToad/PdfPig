namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System;
    using System.Collections.Generic;
    using Tables;

    /// <summary>
    /// Parses TrueType fonts.
    /// </summary>
    public static class TrueTypeFontParser
    {
        /// <summary>
        /// Parse the font from the input data.
        /// </summary>
        public static TrueTypeFont Parse(TrueTypeDataBytes data)
        {
            var version = data.Read32Fixed();
            int numberOfTables = data.ReadUnsignedShort();

            // Read these data points to move to the correct data location.
            // ReSharper disable UnusedVariable
            var searchRange = data.ReadUnsignedShort();
            var entrySelector = data.ReadUnsignedShort();
            var rangeShift = data.ReadUnsignedShort();
            // ReSharper restore UnusedVariable

            var tables = new Dictionary<string, TrueTypeHeaderTable>(StringComparer.OrdinalIgnoreCase);

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

        private static TrueTypeHeaderTable? ReadTable(TrueTypeDataBytes data)
        {
            var tag = data.ReadTag();
            var checksum = data.ReadUnsignedInt();
            var offset = data.ReadUnsignedInt();
            var length = data.ReadUnsignedInt();

            // skip tables with zero length (except glyf)
            if (length == 0 && !string.Equals(tag, TrueTypeHeaderTable.Glyf, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new TrueTypeHeaderTable(tag, checksum, offset, length);
        }

        private static TrueTypeFont ParseTables(float version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tables, TrueTypeDataBytes data)
        {
            var isPostScript = tables.ContainsKey(TrueTypeHeaderTable.Cff);

            var builder = new TableRegister.Builder();

            if (!tables.TryGetValue(TrueTypeHeaderTable.Head, out var table))
            {
                throw new InvalidFontFormatException($"The {TrueTypeHeaderTable.Head} table is required.");
            }

            // head
            builder.HeaderTable = HeaderTable.Load(data, table);

            if (!tables.TryGetValue(TrueTypeHeaderTable.Hhea, out var hHead))
            {
                throw new InvalidFontFormatException("The horizontal header table is required.");
            }

            // hhea
            builder.HorizontalHeaderTable = TableParser.Parse<HorizontalHeaderTable>(hHead, data, builder);

            if (!tables.TryGetValue(TrueTypeHeaderTable.Maxp, out var maxHeaderTable))
            {
                throw new InvalidFontFormatException("The maximum profile table is required.");
            }

            // maxp
            builder.MaximumProfileTable = BasicMaximumProfileTable.Load(data, maxHeaderTable);

            // post
            if (tables.TryGetValue(TrueTypeHeaderTable.Post, out var postscriptHeaderTable))
            {
                builder.PostScriptTable = PostScriptTable.Load(data, postscriptHeaderTable, builder.MaximumProfileTable);
            }

            if (tables.TryGetValue(TrueTypeHeaderTable.Name, out var nameTable))
            {
                builder.NameTable = TableParser.Parse<NameTable>(nameTable, data, builder);
            }

            if (tables.TryGetValue(TrueTypeHeaderTable.Os2, out var os2Table))
            {
                builder.Os2Table = TableParser.Parse<Os2Table>(os2Table, data, builder);
            }
            
            if (!isPostScript)
            {
                if (!tables.TryGetValue(TrueTypeHeaderTable.Loca, out var indexToLocationHeaderTable))
                {
                    throw new InvalidFontFormatException("The location to index table is required for non-PostScript fonts.");
                }

                // loca
                builder.IndexToLocationTable =
                    IndexToLocationTable.Load(data, indexToLocationHeaderTable, builder);

                if (!tables.TryGetValue(TrueTypeHeaderTable.Glyf, out var glyphHeaderTable))
                {
                    throw new InvalidFontFormatException("The glyph table is required for non-PostScript fonts.");
                }

                // glyf
                builder.GlyphDataTable = GlyphDataTable.Load(data, glyphHeaderTable, builder);

                OptionallyParseTables(tables, data, builder);
            }

            return new TrueTypeFont(version, tables, builder.Build(), data.InputBytes.Bytes);
        }

        internal static NameTable GetNameTable(TrueTypeDataBytes data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            
            // Read these data points to move to the correct data location.
            data.Read32Fixed();
            int numberOfTables = data.ReadUnsignedShort();
            data.ReadUnsignedShort();
            data.ReadUnsignedShort();
            data.ReadUnsignedShort();

            TrueTypeHeaderTable? name = null;

            for (var i = 0; i < numberOfTables; i++)
            {
                var tableHeader = ReadTable(data);

                if (tableHeader.HasValue && tableHeader.Value.Tag == TrueTypeHeaderTable.Name)
                {
                    name = tableHeader;
                    break;
                }
            }

            if (!name.HasValue)
            {
                return null;
            }

            return TableParser.Parse<NameTable>(name.Value, data, new TableRegister.Builder());
        }

        private static void OptionallyParseTables(IReadOnlyDictionary<string, TrueTypeHeaderTable> tables, TrueTypeDataBytes data, TableRegister.Builder tableRegister)
        {
            // cmap
            if (tables.TryGetValue(TrueTypeHeaderTable.Cmap, out var cmap))
            {
                tableRegister.CMapTable = TableParser.Parse<CMapTable>(cmap, data, tableRegister);
            }

            // hmtx
            if (tables.TryGetValue(TrueTypeHeaderTable.Hmtx, out var hmtxHeaderTable))
            {
                tableRegister.HorizontalMetricsTable = TableParser.Parse<HorizontalMetricsTable>(hmtxHeaderTable, data, tableRegister);
            }

            // kern
            if (tables.TryGetValue(TrueTypeHeaderTable.Kern, out var kernHeaderTable))
            {
                tableRegister.KerningTable = KerningTable.Load(data, kernHeaderTable);
            }
        }
    }
}

