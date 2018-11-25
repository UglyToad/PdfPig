namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.Collections.Generic;
    using CidFonts;
    using Geometry;
    using Parser;

    internal class TrueTypeFontProgram : ICidFontProgram
    {
        public decimal Version { get; }

        public IReadOnlyDictionary<string, TrueTypeHeaderTable> TableHeaders { get; }

        public TableRegister TableRegister { get; }

        public TrueTypeFontProgram(decimal version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tableHeaders, TableRegister tableRegister)
        {
            Version = version;
            TableHeaders = tableHeaders;
            TableRegister = tableRegister ?? throw new ArgumentNullException(nameof(tableRegister));
        }

        public bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox) => TryGetBoundingBox(characterIdentifier, null, out boundingBox);
        public bool TryGetBoundingBox(int characterIdentifier, Func<int, int> characterIdentifierToGlyphIndex, out PdfRectangle boundingBox)
        {
            boundingBox = default(PdfRectangle);

            if (!TryGetGlyphIndex(characterIdentifier, characterIdentifierToGlyphIndex, out var index))
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

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, out decimal width) => TryGetBoundingAdvancedWidth(characterIdentifier, null, out width);
        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, Func<int, int> characterIdentifierToGlyphIndex, out decimal width)
        {
            width = 0m;

            if (!TryGetGlyphIndex(characterIdentifier, characterIdentifierToGlyphIndex, out var index))
            {
                return false;
            }

            return TryGetBoundingAdvancedWidthByIndex(index, out width);
        }

        public int GetFontMatrixMultiplier()
        {
            return TableRegister.HeaderTable.UnitsPerEm;
        }

        private bool TryGetBoundingAdvancedWidthByIndex(int index, out decimal width)
        {
            width = TableRegister.HorizontalMetricsTable.GetAdvanceWidth(index);

            return true;
        }

        private bool TryGetGlyphIndex(int characterIdentifier, Func<int, int> characterIdentifierToGlyphIndex, out int glyphIndex)
        {
            glyphIndex = 0;

            if (characterIdentifierToGlyphIndex != null)
                {
                    glyphIndex = characterIdentifierToGlyphIndex(characterIdentifier);

                    return true;
                }

            if (TableRegister.CMapTable == null)
            {
                return false;
            }

            return TableRegister.CMapTable.TryGetGlyphIndex(characterIdentifier, out glyphIndex);
        }
    }
}