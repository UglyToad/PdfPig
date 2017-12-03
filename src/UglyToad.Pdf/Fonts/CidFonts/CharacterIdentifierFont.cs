namespace UglyToad.Pdf.Fonts.CidFonts
{
    using Cmap;
    using Cos;

    internal class CharacterIdentifierFont
    {
        public const int DefaultWidthWhenUndeclared = 1000;

        public CidFontType Subtype { get; }

        public CosName BaseFont { get; }

        public CharacterIdentifierSystemInfo SystemInfo { get; set; }

        public CosObjectKey FontDescriptor { get; set; }

        public int DefaultWidth { get; }

        public COSArray Widths { get; set; }

        public VerticalVectorComponents VerticalVectors { get; } = VerticalVectorComponents.Default;

        public CharacterIdentifierToGlyphIdentifierMap CidToGidMap { get; }

        public CharacterIdentifierFont(CidFontType subtype, CosName baseFont, CharacterIdentifierSystemInfo systemInfo,
            CosObjectKey fontDescriptor,
            int defaultWidth,
            COSArray widths,
            CharacterIdentifierToGlyphIdentifierMap cidToGidMap)
        {
            Subtype = subtype;
            BaseFont = baseFont;
            SystemInfo = systemInfo;
            FontDescriptor = fontDescriptor;
            DefaultWidth = defaultWidth;
            Widths = widths;
            CidToGidMap = cidToGidMap;
        }


    }
}