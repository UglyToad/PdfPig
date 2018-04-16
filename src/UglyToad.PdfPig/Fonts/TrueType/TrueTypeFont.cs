namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.Collections.Generic;
    using CidFonts;
    using Geometry;
    using Parser;
    using Tables;

    internal class TrueTypeFont : ICidFontProgram
    {
        public decimal Version { get; }

        public IReadOnlyDictionary<string, TrueTypeHeaderTable> TableHeaders { get; }

        public HeaderTable HeaderTable { get; }
        public CMapTable CMapTable { get; }
        public GlyphDataTable GlyphTable { get; }
        public TableRegister TableRegister { get; }

        public TrueTypeFont(decimal version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tableHeaders, TableRegister tableRegister)
        {
            Version = version;
            TableHeaders = tableHeaders;
            TableRegister = tableRegister ?? throw new ArgumentNullException(nameof(tableRegister));
            HeaderTable = tableRegister.HeaderTable;
            CMapTable = tableRegister.CMapTable;
            GlyphTable = tableRegister.GlyphDataTable;
        }

        public bool TryGetBoundingBox(int characterCode, out PdfRectangle boundingBox) => TryGetBoundingBox(characterCode, null, out boundingBox);
        public bool TryGetBoundingBox(int characterCode, Func<int, int> characterIdentifierToGlyphIndex, out PdfRectangle boundingBox)
        {
            boundingBox = default(PdfRectangle);

            if (!TryGetGlyphIndex(characterCode, characterIdentifierToGlyphIndex, out var index))
            {
                return false;
            }

            var glyph = GlyphTable.Glyphs[index];

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

        public bool TryGetBoundingAdvancedWidth(int characterCode, out decimal width)
        {
            width = 0m;

            if (!TryGetGlyphIndex(characterCode, null, out var index))
            {
                return false;
            }

            return TryGetBoundingAdvancedWidthByIndex(index, out width);
        }

        public int GetFontMatrixMultiplier()
        {
            return HeaderTable?.UnitsPerEm ?? 1000;
        }

        private bool TryGetBoundingAdvancedWidthByIndex(int index, out decimal width)
        {
            width = TableRegister.HorizontalMetricsTable.GetAdvanceWidth(index);

            return true;
        }

        private bool TryGetGlyphIndex(int characterIdentifier, Func<int, int> characterIdentifierToGlyphIndex, out int glyphIndex)
        {
            glyphIndex = 0;

            if (CMapTable == null)
            {
                if (characterIdentifierToGlyphIndex == null)
                {
                    return false;
                }

                glyphIndex = characterIdentifierToGlyphIndex(characterIdentifier);
            }
            else if (!CMapTable.TryGetGlyphIndex(characterIdentifier, out glyphIndex))
            {
                return false;
            }

            return true;
        }
    }
}