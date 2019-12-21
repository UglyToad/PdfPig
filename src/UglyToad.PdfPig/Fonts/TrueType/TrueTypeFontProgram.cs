namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.Collections.Generic;
    using CidFonts;
    using Geometry;
    using Parser;
    using Tables.CMapSubTables;
    using Util.JetBrains.Annotations;

    internal class TrueTypeFontProgram : ICidFontProgram
    {
        public decimal Version { get; }

        public IReadOnlyDictionary<string, TrueTypeHeaderTable> TableHeaders { get; }

        [NotNull]
        public TableRegister TableRegister { get; }

        [CanBeNull]
        public string Name => TableRegister.NameTable?.FontName;

        public ICMapSubTable WindowsUnicodeCMap { get; }

        public ICMapSubTable MacRomanCMap { get; }

        public ICMapSubTable WindowsSymbolCMap { get; }

        public TrueTypeFontProgram(decimal version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tableHeaders, TableRegister tableRegister)
        {
            Version = version;
            TableHeaders = tableHeaders;
            TableRegister = tableRegister ?? throw new ArgumentNullException(nameof(tableRegister));

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

        public bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox) => TryGetBoundingBox(characterIdentifier, null, out boundingBox);
        public bool TryGetBoundingBox(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out PdfRectangle boundingBox)
        {
            boundingBox = default(PdfRectangle);

            if (!TryGetGlyphIndex(characterIdentifier, characterCodeToGlyphId, out var index)
                || TableRegister.GlyphTable == null)
            {
                return false;
            }

            var glyph = TableRegister.GlyphTable.Glyphs[index];

            if (glyph?.Bounds == null)
            {
                return false;
            }

            if (glyph.IsEmpty && TryGetBoundingAdvancedWidthByIndex(index, out var advanceWidth))
            {
                boundingBox = new PdfRectangle(0, 0, advanceWidth, 0);
            }
            else
            {
                boundingBox = glyph.Bounds;
            }

            return true;
        }

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, out double width) => TryGetBoundingAdvancedWidth(characterIdentifier, null, out width);
        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out double width)
        {
            width = 0.0;

            if (!TryGetGlyphIndex(characterIdentifier, characterCodeToGlyphId, out var index))
            {
                return false;
            }

            return TryGetBoundingAdvancedWidthByIndex(index, out width);
        }

        public int GetFontMatrixMultiplier()
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