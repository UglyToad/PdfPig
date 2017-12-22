namespace UglyToad.Pdf.Fonts.CidFonts
{
    using Cos;

    /// <inheritdoc />
    /// <summary>
    /// Type 2 CID fonts contains glyph descriptions based on
    /// the TrueType font format.
    /// </summary>
    internal class Type2CidFont : ICidFont
    {
        public CosName Type { get; }
        public CosName SubType { get; }
        public CosName BaseFont { get; }
        public CharacterIdentifierSystemInfo SystemInfo { get; }
        public CidFontType CidFontType => CidFontType.Type2;
    }
}