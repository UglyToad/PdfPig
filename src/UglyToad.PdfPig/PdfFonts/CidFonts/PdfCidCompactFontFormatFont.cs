namespace UglyToad.PdfPig.PdfFonts.CidFonts
{
    using System;
    using System.Linq;
    using Core;
    using Fonts.CompactFontFormat;

    internal class PdfCidCompactFontFormatFont : ICidFontProgram
    {
        private readonly CompactFontFormatFontCollection fontCollection;

        public PdfCidCompactFontFormatFont(CompactFontFormatFontCollection fontCollection)
        {
            this.fontCollection = fontCollection;
        }

        public TransformationMatrix GetFontTransformationMatrix() => fontCollection.GetFirstTransformationMatrix();

        public PdfRectangle? GetCharacterBoundingBox(string characterName) => fontCollection.GetCharacterBoundingBox(characterName);

        public bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox)
        {
            var font = GetFont();

            var characterName = GetCharacterName(characterIdentifier);

            boundingBox = font.GetCharacterBoundingBox(characterName) ?? new PdfRectangle(0, 0, 500, 0);

            return true;
        }


        public bool TryGetBoundingBox(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out PdfRectangle boundingBox)
        {
            throw new NotImplementedException();
        }

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out double width)
        {
            throw new NotImplementedException();
        }

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, out double width)
        {
            throw new NotImplementedException();
        }

        public int GetFontMatrixMultiplier()
        {
            return 1000;
        }

        public string GetCharacterName(int characterCode)
        {
            var font = GetFont();

            if (font.Encoding != null)
            {
                return font.Encoding.GetName(characterCode);
            }

            return ".notdef";
        }

        private CompactFontFormatFont GetFont()
        {
#if DEBUG
            // TODO: what to do if there are multiple fonts?
            if (fontCollection.Fonts.Count > 1)
            {
                throw new NotSupportedException("Multiple fonts in a CFF");
            }
#endif
            return fontCollection.FirstFont;
        }
    }
}
