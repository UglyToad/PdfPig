namespace UglyToad.PdfPig.PdfFonts.CidFonts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;

    /// <summary>
    /// Represents either an Adobe Type 1 or TrueType font program for a CIDFont.
    /// </summary>
    internal interface ICidFontProgram
    {
        FontDetails Details { get; }

        bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox);

        bool TryGetBoundingBox(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out PdfRectangle boundingBox);

        bool TryGetBoundingAdvancedWidth(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out double width);

        bool TryGetBoundingAdvancedWidth(int characterIdentifier, out double width);

        double? GetDescent();

        double? GetAscent();

        bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path);

        bool TryGetPath(int characterCode, Func<int, int?> characterCodeToGlyphId, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path);

        int GetFontMatrixMultiplier();

        /// <summary>
        /// Try to get the font matrix if available.
        /// </summary>
        bool TryGetFontMatrix(int characterCode, [NotNullWhen(true)] out TransformationMatrix? matrix);
    }
}
