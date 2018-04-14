namespace UglyToad.PdfPig.Fonts.CidFonts
{
    using Geometry;

    /// <summary>
    /// Represents either an Adobe Type 1 or TrueType font program for a CIDFont.
    /// </summary>
    internal interface ICidFontProgram
    {
        bool TryGetBoundingBox(int characterCode, out PdfRectangle boundingBox);

        bool TryGetBoundingAdvancedWidth(int characterCode, out decimal width);
    }
}
