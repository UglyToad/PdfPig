namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    /// <summary>
    /// In a TrueType font the CMap table maps from character codes to glyph indices
    /// A font which can run on multiple platforms will have multiple encoding tables. These are stored as multiple
    /// sub-tables. The <see cref="ICMapSubTable"/> represents a single subtotal.
    /// </summary>
    internal interface ICMapSubTable
    {
        /// <summary>
        /// The platform identifier.
        /// </summary>
        /// <remarks>
        /// 0: Unicode
        /// 1: Macintosh
        /// 2: Reserved
        /// 3: Microsoft
        /// </remarks>
        int PlatformId { get; }

        /// <summary>
        /// Platform specific encoding indentifier.
        /// </summary>
        int EncodingId { get; }

        /// <summary>
        /// Maps from a character code to the array index of the glyph in the font data.
        /// </summary>
        /// <param name="characterCode">The character code.</param>
        /// <returns>The index of the glyph information for this character.</returns>
        int CharacterCodeToGlyphIndex(int characterCode);
    }
}