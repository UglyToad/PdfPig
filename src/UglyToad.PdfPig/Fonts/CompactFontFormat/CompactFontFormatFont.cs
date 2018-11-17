namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using Charsets;
    using CharStrings;
    using Dictionaries;
    using Type1.CharStrings;
    using Util;

    internal class CompactFontFormatFont
    {
        private readonly CompactFontFormatTopLevelDictionary topDictionary;
        private readonly CompactFontFormatPrivateDictionary privateDictionary;
        private readonly ICompactFontFormatCharset charset;
        private readonly Union<Type1CharStrings, Type2CharStrings> charStrings;

        public CompactFontFormatFont(CompactFontFormatTopLevelDictionary topDictionary, CompactFontFormatPrivateDictionary privateDictionary, 
            ICompactFontFormatCharset charset, 
            Union<Type1CharStrings, Type2CharStrings> charStrings)
        {
            this.topDictionary = topDictionary;
            this.privateDictionary = privateDictionary;
            this.charset = charset;
            this.charStrings = charStrings;
        }
    }
}
