namespace UglyToad.PdfPig.Fonts.CidFonts
{
    using Core;
    using Geometry;
    using Tokenization.Tokens;

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
        /// <see cref="NameToken.Font"/>
        /// </summary>
        NameToken Type { get; }

        /// <summary>
        /// Either Type0 (Adobe Type 1 font) or Type2 (TrueType font).
        /// </summary>
        NameToken SubType { get; }

        /// <summary>
        /// The PostScript name of the CIDFont.
        /// </summary>
        NameToken BaseFont { get; }

        /// <summary>
        /// The definition of the character collection for the font.
        /// </summary>
        CharacterIdentifierSystemInfo SystemInfo { get; }

        TransformationMatrix FontMatrix { get; }

        CidFontType CidFontType { get; }

        FontDescriptor Descriptor { get; }

        decimal GetWidthFromDictionary(int cid);

        PdfRectangle GetBoundingBox(int characterIdentifier);
    }
}