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

        public TrueTypeFont(decimal version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tableHeaders, TableRegister tableRegister)
        {
            if (tableRegister == null)
            {
                throw new ArgumentNullException(nameof(tableRegister));
            }

            Version = version;
            TableHeaders = tableHeaders;
            HeaderTable = tableRegister.HeaderTable;
            CMapTable = tableRegister.CMapTable;
            GlyphTable = tableRegister.GlyphDataTable;
        }

        public bool TryGetBoundingBox(int characterCode, out PdfRectangle boundingBox) => TryGetBoundingBox(characterCode, null, out boundingBox);
        public bool TryGetBoundingBox(int characterCode, Func<int, int> characterIdentifierToGlyphIndex, out PdfRectangle boundingBox)
        {
            boundingBox = default(PdfRectangle);

            int index;

            if (CMapTable == null)
            {
                if (characterIdentifierToGlyphIndex == null)
                {
                    return false;
                }

                index = characterIdentifierToGlyphIndex(characterCode);
            }
            else if (!CMapTable.TryGetGlyphIndex(characterCode, out index))
            {
                return false;
            }

            var glyph = GlyphTable.Glyphs[index];

            if (glyph?.Bounds == null)
            {
                return false;
            }

            boundingBox = glyph.Bounds;

            return true;
        }
    }
}