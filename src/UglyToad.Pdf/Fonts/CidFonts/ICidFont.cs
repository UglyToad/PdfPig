namespace UglyToad.Pdf.Fonts.CidFonts
{
    using Cmap;
    using Cos;

    /// <summary>
    /// A CID font contains glyph descriptions accessed by 
    /// CID (character identifier) as character selectors.
    /// </summary>
    /// <remarks>
    /// A CID font contains information about a CIDFont program but is
    /// not itself a font. It can only be a descendant of a Type 0 font.
    /// </remarks>
    internal interface ICidFont
    {
        /// <summary>
        /// <see cref="CosName.FONT"/>
        /// </summary>
        CosName Type { get; }

        /// <summary>
        /// Either Type0 (Adobe Type 1 font) or Type2 (TrueType font).
        /// </summary>
        CosName SubType { get; }

        /// <summary>
        /// The PostScript name of the CIDFont.
        /// </summary>
        CosName BaseFont { get; }

        /// <summary>
        /// The definition of the character collection for the font.
        /// </summary>
        CharacterIdentifierSystemInfo SystemInfo { get; }
    }
}