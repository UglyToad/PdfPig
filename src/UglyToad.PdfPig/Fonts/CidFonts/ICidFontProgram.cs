namespace UglyToad.PdfPig.Fonts.CidFonts
{
    using System;
    using Geometry;

    /// <summary>
    /// Represents either an Adobe Type 1 or TrueType font program for a CIDFont.
    /// </summary>
    internal interface ICidFontProgram
    {
        bool TryGetBoundingBox(int characterCode, out PdfRectangle boundingBox);
        bool TryGetBoundingBox(int characterCode, Func<int, int> characterIdentifierToGlyphIndex, out PdfRectangle boundingBox);
    }
}
