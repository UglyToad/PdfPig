namespace UglyToad.PdfPig.PdfFonts.CidFonts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Fonts;
    using Fonts.CompactFontFormat;

    internal sealed class PdfCidCompactFontFormatFont : ICidFontProgram
    {
        private readonly CompactFontFormatFontCollection fontCollection;

        public FontDetails Details { get; }

        public PdfCidCompactFontFormatFont(CompactFontFormatFontCollection fontCollection)
        {
            this.fontCollection = fontCollection;
            Details = GetDetails(fontCollection?.FirstFont);
        }

        private static FontDetails GetDetails(CompactFontFormatFont? font)
        {
            if (font is null)
            {
                return FontDetails.GetDefault();
            }

            FontDetails WithWeightValues(bool isBold, int weight) => new FontDetails(null, isBold, weight, font.ItalicAngle != 0);

            return (font.Weight?.ToLowerInvariant()) switch
            {
                "light"    => WithWeightValues(false, 300),
                "semibold" => WithWeightValues(true, 600),
                "bold"     => WithWeightValues(true, FontDetails.BoldWeight),
                "black"    => WithWeightValues(true, 900),
                _          => WithWeightValues(false, FontDetails.DefaultWeight)
            };
        }

        public TransformationMatrix GetFontTransformationMatrix() => fontCollection.GetFirstTransformationMatrix();

        public PdfRectangle? GetCharacterBoundingBox(string characterName) => fontCollection.GetCharacterBoundingBox(characterName);

        public double? GetDescent()
        {
            // BobLd: we don't support ascent / descent for cff for the moment
            return null;
        }

        public double? GetAscent()
        {
            // BobLd: we don't support ascent / descent for cff for the moment
            return null;
        }

        public bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox)
        {
            boundingBox = new PdfRectangle(0, 0, 500, 0);

            var font = GetFont();

            var characterName = GetCharacterName(characterIdentifier);

            if (string.Equals(characterName, GlyphList.NotDefined, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            boundingBox = font.GetCharacterBoundingBox(characterName) ?? new PdfRectangle(0, 0, 500, 0);

            return true;
        }

        public bool TryGetBoundingBox(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out PdfRectangle boundingBox)
        {
            var font = GetFont();

            int? glyphId = characterCodeToGlyphId.Invoke(characterIdentifier);

            string name = glyphId.HasValue
                ? GetCharacterName(glyphId.Value)
                : GetCharacterName(characterIdentifier);

            PdfRectangle? tempBbox = font.GetCharacterBoundingBox(name);
            if (tempBbox.HasValue)
            {
                boundingBox = tempBbox.Value;
                return true;
            }

            boundingBox = default;
            return false;
        }

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out double width)
        {
            return TryGetBoundingAdvancedWidth(characterIdentifier, out width);
        }

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, out double width)
        {
            width = double.NaN;
            return false;
        }

        public int GetFontMatrixMultiplier()
        {
            return 1000;
        }

        public bool TryGetFontMatrix(int characterCode, [NotNullWhen(true)] out TransformationMatrix? matrix)
        {
            var font = GetFont();
            var name = font.GetCharacterName(characterCode, true);
            if (name is null)
            {
                matrix = null;
                return false;
            }
            matrix = font.GetFontMatrix(name);
            return matrix.HasValue;
        }

        public string GetCharacterName(int characterCode)
        {
            var font = GetFont();

            var name = font.GetCharacterName(characterCode, true);

            return name ?? GlyphList.NotDefined;
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

        public bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            path = null;

            var font = GetFont();

            var characterName = GetCharacterName(characterCode);

            if (string.Equals(characterName, GlyphList.NotDefined, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (font.TryGetPath(characterName, out path))
            {
                return true;
            }

            return false;
        }

        public bool TryGetPath(int characterCode, Func<int, int?> characterCodeToGlyphId, out IReadOnlyList<PdfSubpath> path)
        {
            path = null;

            int? glyphId = characterCodeToGlyphId.Invoke(characterCode);

            string characterName = glyphId.HasValue
                ? GetCharacterName(glyphId.Value)
                : GetCharacterName(characterCode);

            if (string.Equals(characterName, GlyphList.NotDefined, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (GetFont().TryGetPath(characterName, out path))
            {
                return true;
            }

            return false;
        }
    }
}
