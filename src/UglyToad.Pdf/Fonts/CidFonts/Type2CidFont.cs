namespace UglyToad.Pdf.Fonts.CidFonts
{
    using System.Collections.Generic;
    using Cos;

    /// <inheritdoc />
    /// <summary>
    /// Type 2 CID fonts contains glyph descriptions based on
    /// the TrueType font format.
    /// </summary>
    internal class Type2CidFont : ICidFont
    {
        private readonly ICidFontProgram fontProgram;
        private readonly VerticalWritingMetrics verticalWritingMetrics;
        private readonly IReadOnlyDictionary<int, decimal> widths;

        public CosName Type { get; }
        public CosName SubType { get; }
        public CosName BaseFont { get; }
        public CharacterIdentifierSystemInfo SystemInfo { get; }
        public CidFontType CidFontType => CidFontType.Type2;
        public FontDescriptor Descriptor { get; }

        public Type2CidFont(CosName type, CosName subType, CosName baseFont, CharacterIdentifierSystemInfo systemInfo, 
            FontDescriptor descriptor, ICidFontProgram fontProgram,
            VerticalWritingMetrics verticalWritingMetrics,
            IReadOnlyDictionary<int, decimal> widths)
        {
            Type = type;
            SubType = subType;
            BaseFont = baseFont;
            SystemInfo = systemInfo;
            Descriptor = descriptor;
            this.fontProgram = fontProgram;
            this.verticalWritingMetrics = verticalWritingMetrics;
            this.widths = widths;
        }

        public decimal GetWidthFromFont(int characterCode)
        {
            throw new System.NotImplementedException();
        }

        public decimal GetWidthFromDictionary(int cid)
        {
            if (widths.TryGetValue(cid, out var width))
            {
                return width;
            }

            return Descriptor.MissingWidth;
        }
    }
}