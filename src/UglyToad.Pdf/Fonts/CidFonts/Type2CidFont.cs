namespace UglyToad.Pdf.Fonts.CidFonts
{
    using Cos;
    using TrueType.Parser;

    /// <inheritdoc />
    /// <summary>
    /// Type 2 CID fonts contains glyph descriptions based on
    /// the TrueType font format.
    /// </summary>
    internal class Type2CidFont : ICidFont
    {
        private readonly TrueTypeFont fontProgram;
        public CosName Type { get; }
        public CosName SubType { get; }
        public CosName BaseFont { get; }
        public CharacterIdentifierSystemInfo SystemInfo { get; }
        public CidFontType CidFontType => CidFontType.Type2;
        public FontDescriptor Descriptor { get; }

        public Type2CidFont(CosName type, CosName subType, CosName baseFont, CharacterIdentifierSystemInfo systemInfo, FontDescriptor descriptor, TrueTypeFont fontProgram)
        {
            Type = type;
            SubType = subType;
            BaseFont = baseFont;
            SystemInfo = systemInfo;
            Descriptor = descriptor;
            this.fontProgram = fontProgram;
        }
    }
}