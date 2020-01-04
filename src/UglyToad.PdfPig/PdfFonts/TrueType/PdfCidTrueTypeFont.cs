namespace UglyToad.PdfPig.PdfFonts.TrueType
{
    using System;
    using CidFonts;
    using Core;
    using Fonts.TrueType;

    internal class PdfCidTrueTypeFont : ICidFontProgram
    {
        private readonly TrueTypeFont font;
        
        public PdfCidTrueTypeFont(TrueTypeFont font)
        {
            this.font = font ?? throw new ArgumentNullException(nameof(font));
        }

        public bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox) => TryGetBoundingBox(characterIdentifier, null, out boundingBox);

        public bool TryGetBoundingBox(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out PdfRectangle boundingBox)
            => font.TryGetBoundingBox(characterIdentifier, characterCodeToGlyphId, out boundingBox);

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, out double width) => TryGetBoundingAdvancedWidth(characterIdentifier, null, out width);

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out double width)
            => font.TryGetAdvanceWidth(characterIdentifier, characterCodeToGlyphId, out width);

        public int GetFontMatrixMultiplier() => font.GetUnitsPerEm();
    }
}