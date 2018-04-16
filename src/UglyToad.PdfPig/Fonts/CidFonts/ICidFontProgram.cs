namespace UglyToad.PdfPig.Fonts.CidFonts
{
    using System;
    using Geometry;

    /// <summary>
    /// Represents either an Adobe Type 1 or TrueType font program for a CIDFont.
    /// </summary>
    internal interface ICidFontProgram
    {
        bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox);

        bool TryGetBoundingBox(int characterIdentifier, Func<int, int> characterIdentifierToGlyphIndex, out PdfRectangle boundingBox);

        bool TryGetBoundingAdvancedWidth(int characterCode, out decimal width);

        int GetFontMatrixMultiplier();
    }
}
