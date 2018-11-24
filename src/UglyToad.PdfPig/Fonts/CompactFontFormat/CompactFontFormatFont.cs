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
        public CompactFontFormatPrivateDictionary PrivateDictionary { get; }
        public ICompactFontFormatCharset Charset { get; }
        public Union<Type1CharStrings, Type2CharStrings> CharStrings { get; }

        public CompactFontFormatFont(CompactFontFormatTopLevelDictionary topDictionary, CompactFontFormatPrivateDictionary privateDictionary, 
            ICompactFontFormatCharset charset, 
            Union<Type1CharStrings, Type2CharStrings> charStrings)
        {
            TopDictionary = topDictionary;
            PrivateDictionary = privateDictionary;
            Charset = charset;
            CharStrings = charStrings;
        }

        public PdfRectangle? GetCharacterBoundingBox(string characterName)
        {
            var result = default(PdfRectangle?);
            CharStrings.Match(x => throw new NotImplementedException("Type 1 CharStrings in a CFF font are currently unsupported."),
                x => { result = x.Generate(characterName).Path.GetBoundingRectangle(); });

            return result;
        }
    }
}
