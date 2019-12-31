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
        TrueTypeCMapPlatform PlatformId { get; }

        /// <summary>
        /// Platform specific encoding indentifier. Interpretation depends on the value of the <see cref="PlatformId"/>.
        /// </summary>
        ushort EncodingId { get; }
        
        /// <summary>
        /// Maps from a character code to the array index of the glyph in the font data.
        /// </summary>
        /// <param name="characterCode">The character code.</param>
        /// <returns>The index of the glyph information for this character.</returns>
        int CharacterCodeToGlyphIndex(int characterCode);
    }

    /// <summary>
    /// The platform identifier for a CMap table.
    /// </summary>
    internal enum TrueTypeCMapPlatform : ushort
    {
        /// <summary>
        /// Unicode.
        /// </summary>
        Unicode = 0,
        /// <summary>
        /// Apple Macintosh.
        /// </summary>
        Macintosh = 1,
        /// <summary>
        /// Unused.
        /// </summary>
        Reserved2 = 2,
        /// <summary>
        /// Microsoft Windows.
        /// </summary>
        Windows = 3
    }
}