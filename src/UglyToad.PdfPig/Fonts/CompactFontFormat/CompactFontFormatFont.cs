namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using Charsets;
    using CharStrings;
    using Dictionaries;
    using Geometry;
    using Type1.CharStrings;
    using Util;

    internal class CompactFontFormatFont
    {
        public CompactFontFormatTopLevelDictionary TopDictionary { get; }
        private readonly CompactFontFormatPrivateDictionary privateDictionary;
        private readonly ICompactFontFormatCharset charset;
        private readonly Union<Type1CharStrings, Type2CharStrings> charStrings;

        public CompactFontFormatFont(CompactFontFormatTopLevelDictionary topDictionary, CompactFontFormatPrivateDictionary privateDictionary, 
            ICompactFontFormatCharset charset, 
            Union<Type1CharStrings, Type2CharStrings> charStrings)
        {
            TopDictionary = topDictionary;
            this.privateDictionary = privateDictionary;
            this.charset = charset;
            this.charStrings = charStrings;
        }

        public PdfRectangle? GetCharacterBoundingBox(string characterName)
        {
            var result = default(PdfRectangle?);
            charStrings.Match(x => throw new NotImplementedException("Type 1 CharStrings in a CFF font are currently unsupported."),
                x => { result = x.Generate(characterName).GetBoundingRectangle(); });

            return result;
        }
    }
}
