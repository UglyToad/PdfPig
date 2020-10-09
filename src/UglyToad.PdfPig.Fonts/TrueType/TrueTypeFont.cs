namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Parser;
    using Tables.CMapSubTables;

    /// <summary>
    /// A TrueType font.
    /// </summary>
    public class TrueTypeFont
    {
        /// <summary>
        /// FontFileBytes
        /// </summary>
        public IReadOnlyList<byte> FontFileBytes { get; }

        /// <summary>
        /// The font version number.
        /// </summary>
        public float Version { get; }

        /// <summary>
        /// The table directory, entries indicate the offset and length of the data for a given table name.
        /// </summary>
        public IReadOnlyDictionary<string, TrueTypeHeaderTable> TableHeaders { get; }

        /// <summary>
        /// The actual table data parsed for this TrueType font.
        /// </summary>
        public TableRegister TableRegister { get; }

        /// <summary>
        /// The name of the font according to the font's name table.
        /// </summary>
        public string Name => TableRegister.NameTable?.FontName;

        /// <summary>
        /// The cmap subtable for Windows Unicode (3, 1).
        /// Can be <see langword="null" />.
        /// </summary>
        public ICMapSubTable WindowsUnicodeCMap { get; }

        /// <summary>
        /// The cmap subtable for Mac Roman (1, 0).
        /// Can be <see langword="null" />.
        /// </summary>
        public ICMapSubTable MacRomanCMap { get; }

        /// <summary>
        /// The cmap subtable for Windows Symbol (3, 0).
        /// Can be <see langword="null" />.
        /// </summary>
        public ICMapSubTable WindowsSymbolCMap { get; }

        /// <summary>
        /// The number of tables in this font.
        /// </summary>
        public int NumberOfTables { get; }

        /// <summary>
        /// Create a new <see cref="TrueTypeFont"/>.
        /// </summary>
        internal TrueTypeFont(float version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tableHeaders, TableRegister tableRegister, IReadOnlyList<byte> fontFileBytes)
        {
            Version = version;
            TableHeaders = tableHeaders ?? throw new ArgumentNullException(nameof(tableHeaders));
            TableRegister = tableRegister ?? throw new ArgumentNullException(nameof(tableRegister));
            NumberOfTables = tableHeaders.Count;
            FontFileBytes = fontFileBytes;

            if (TableRegister.CMapTable != null)
            {
                const int encodingSymbol = 0;
                const int encodingUnicode = 1;
                const int encodingMacRoman = 0;

                foreach (var subTable in TableRegister.CMapTable.SubTables)
                {
                    if (WindowsSymbolCMap == null
                        && subTable.PlatformId == TrueTypeCMapPlatform.Windows
                        && subTable.EncodingId == encodingSymbol)
                    {
                        WindowsSymbolCMap = subTable;
                    }
                    else if (WindowsUnicodeCMap == null
                             && subTable.PlatformId == TrueTypeCMapPlatform.Windows
                             && subTable.EncodingId == encodingUnicode)
                    {
                        WindowsUnicodeCMap = subTable;
                    }
                    else if (MacRomanCMap == null
                             && subTable.PlatformId == TrueTypeCMapPlatform.Macintosh
                             && subTable.EncodingId == encodingMacRoman)
                    {
                        MacRomanCMap = subTable;
                    }
                }
            }
        }

        /// <summary>
        /// Try to get the bounding box for a glyph representing the specified character code if present.
        /// </summary>
        public bool TryGetBoundingBox(int characterCode, out PdfRectangle boundingBox) => TryGetBoundingBox(characterCode, null, out boundingBox);

        /// <summary>
        /// Try to get the bounding box for a glyph representing the specified character code if present.
        /// Uses a custom mapping of character code to glyph index.
        /// </summary>
        public bool TryGetBoundingBox(int characterCode, Func<int, int?> characterCodeToGlyphId, out PdfRectangle boundingBox)
        {
            boundingBox = default(PdfRectangle);

            if (!TryGetGlyphIndex(characterCode, characterCodeToGlyphId, out var index)
                || TableRegister.GlyphTable == null)
            {
                return false;
            }
            
            if (!TableRegister.GlyphTable.TryGetGlyphBounds(index, out boundingBox))
            {
                return false;
            }

            if (boundingBox.Width.Equals(0) && TryGetBoundingAdvancedWidthByIndex(index, out var advanceWidth))
            {
                boundingBox = new PdfRectangle(0, 0, advanceWidth, 0);
            }

            return true;
        }

        /// <summary>
        /// Try to get the advance width for a glyph representing the specified character code if present.
        /// </summary>
        public bool TryGetAdvanceWidth(int characterCode, out double width) => TryGetAdvanceWidth(characterCode, null, out width);

        /// <summary>
        /// Try to get the advance width for a glyph representing the specified character code if present.
        /// </summary>
        public bool TryGetAdvanceWidth(int characterCode, Func<int, int?> characterCodeToGlyphId, out double width)
        {
            width = 0.0;

            if (!TryGetGlyphIndex(characterCode, characterCodeToGlyphId, out var index))
            {
                return false;
            }

            return TryGetBoundingAdvancedWidthByIndex(index, out width);
        }

        /// <summary>
        /// Get the number of units per em for this font.
        /// </summary>
        public int GetUnitsPerEm()
        {
            return TableRegister.HeaderTable.UnitsPerEm;
        }

        private bool TryGetBoundingAdvancedWidthByIndex(int index, out double width)
        {
            width = 0;

            if (TableRegister.HorizontalMetricsTable == null)
            {
                return false;
            }

            width = TableRegister.HorizontalMetricsTable.GetAdvanceWidth(index);

            return true;
        }

        private bool TryGetGlyphIndex(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out int glyphId)
        {
            glyphId = 0;

            var externalGlyphId = characterCodeToGlyphId?.Invoke(characterIdentifier);

            if (externalGlyphId != null)
            {
                glyphId = externalGlyphId.Value;
                return true;
            }

            if (TableRegister.CMapTable == null)
            {
                return false;
            }

            return TableRegister.CMapTable.TryGetGlyphIndex(characterIdentifier, out glyphId);
        }
    }
}