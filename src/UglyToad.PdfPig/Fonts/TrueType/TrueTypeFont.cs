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
            if (tableRegister == null)
            {
                throw new ArgumentNullException(nameof(tableRegister));
            }

            Version = version;
            TableHeaders = tableHeaders;
            TableRegister = tableRegister;
            HeaderTable = tableRegister.HeaderTable;
            CMapTable = tableRegister.CMapTable;
            GlyphTable = tableRegister.GlyphDataTable;
        }

        public bool TryGetBoundingBox(int characterCode, out PdfRectangle boundingBox)
        {
            boundingBox = default(PdfRectangle);

            if (CMapTable == null)
            {
                return false;
            }

            if (!CMapTable.TryGetGlyphIndex(characterCode, out var index))
            {
                return false;
            }

            var glyph = GlyphTable.Glyphs[index];

            if (glyph?.GlyphBounds == null)
            {
                return false;
            }

            boundingBox = glyph.GlyphBounds;

            return true;
        }

        public bool TryGetBoundingAdvancedWidth(int characterCode, out decimal width)
        {
            width = 0m;

            if (CMapTable == null)
            {
                return false;
            }

            if (!CMapTable.TryGetGlyphIndex(characterCode, out var index))
            {
                return false;
            }

            width = TableRegister.HorizontalMetricsTable.GetAdvanceWidth(index);

            return true;
        }
    }
}