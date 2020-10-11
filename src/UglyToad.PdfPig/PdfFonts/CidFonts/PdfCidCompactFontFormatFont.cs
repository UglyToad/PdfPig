namespace UglyToad.PdfPig.PdfFonts.CidFonts
{
    using System;
    using Core;
    using Fonts.CompactFontFormat;

    internal class PdfCidCompactFontFormatFont : ICidFontProgram
    {
        private readonly CompactFontFormatFontCollection fontCollection;

        public FontDetails Details { get; }

        public PdfCidCompactFontFormatFont(CompactFontFormatFontCollection fontCollection)
        {
            this.fontCollection = fontCollection;
            Details = GetDetails(fontCollection?.FirstFont);
        }

        private static FontDetails GetDetails(CompactFontFormatFont font)
        {
            if (font == null)
            {
                return FontDetails.GetDefault();
            }

            FontDetails WithWeightValues(bool isbold, int weight) => new FontDetails(null, null, isbold, weight, font.ItalicAngle != 0);

            switch (font.Weight?.ToLowerInvariant())
            {
                case "light":
                    return WithWeightValues(false, 300);
                case "semibold":
                    return WithWeightValues(true, 600);
                case "bold":
                    return WithWeightValues(true, FontDetails.BoldWeight);
                case "black":
                    return WithWeightValues(true, 900);
                default:
                    return WithWeightValues(false, FontDetails.DefaultWeight);
            }
        }

        public TransformationMatrix GetFontTransformationMatrix() => fontCollection.GetFirstTransformationMatrix();

        public PdfRectangle? GetCharacterBoundingBox(string characterName) => fontCollection.GetCharacterBoundingBox(characterName);

        public bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox)
        {
            boundingBox = new PdfRectangle(0, 0, 500, 0);

            var font = GetFont();

            if (font.Encoding == null)
            {
                return false;
            }

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
