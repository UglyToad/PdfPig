namespace UglyToad.PdfPig.Fonts.CidFonts
{
    using Core;
    using Cos;

    /// <inheritdoc/>
    /// <summary>
    /// Type 0 CID fonts contain glyph descriptions based on the
    /// Adobe Type 1 font format.
    /// </summary>
    internal class Type0CidFont : ICidFont
    {
        public CosName Type { get; }
        public CosName SubType { get; }
        public CosName BaseFont { get; }
        public CharacterIdentifierSystemInfo SystemInfo { get; }
        public TransformationMatrix FontMatrix { get; }
        public CidFontType CidFontType => CidFontType.Type0;
        public FontDescriptor Descriptor { get; }

        public Type0CidFont()
        {
            throw new System.NotImplementedException();
        }

        public decimal GetWidthFromFont(int characterCode)
        {
            throw new System.NotImplementedException();
        }

        public decimal GetWidthFromDictionary(int cid)
        {
            throw new System.NotImplementedException();
        }
    }
}
