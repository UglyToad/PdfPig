namespace UglyToad.PdfPig.PdfFonts.CidFonts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Fonts.TrueType;
    using Fonts.TrueType.Tables;

    internal sealed class PdfCidTrueTypeFont : ICidFontProgram
    {
        private readonly TrueTypeFont font;

        public FontDetails Details { get; }

        public PdfCidTrueTypeFont(TrueTypeFont font)
        {
            this.font = font ?? throw new ArgumentNullException(nameof(font));

            var header = font.TableRegister.HeaderTable;
            var isBold = header.MacStyle.HasFlag(HeaderTable.HeaderMacStyle.Bold);
            Details = new FontDetails(font.Name, isBold,
                isBold ? FontDetails.BoldWeight : FontDetails.DefaultWeight,
                header.MacStyle.HasFlag(HeaderTable.HeaderMacStyle.Italic));
        }

        public bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox) => TryGetBoundingBox(characterIdentifier, null, out boundingBox);

        public bool TryGetBoundingBox(int characterIdentifier, Func<int, int?>? characterCodeToGlyphId, out PdfRectangle boundingBox)
            => font.TryGetBoundingBox(characterIdentifier, characterCodeToGlyphId, out boundingBox);

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, out double width) => TryGetBoundingAdvancedWidth(characterIdentifier, null, out width);

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, Func<int, int?>? characterCodeToGlyphId, out double width)
            => font.TryGetAdvanceWidth(characterIdentifier, characterCodeToGlyphId, out width);

        public int GetFontMatrixMultiplier() => font.GetUnitsPerEm();

        public bool TryGetFontMatrix(int characterCode, [NotNullWhen(true)] out TransformationMatrix? matrix)
        {
            // We don't have a matrix here
            matrix = null;
            return false;
        }

        public bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path) => font.TryGetPath(characterCode, out path);

        public bool TryGetPath(int characterCode, Func<int, int?> characterCodeToGlyphId, out IReadOnlyList<PdfSubpath> path)
            => font.TryGetPath(characterCode, characterCodeToGlyphId, out path);
    }
}