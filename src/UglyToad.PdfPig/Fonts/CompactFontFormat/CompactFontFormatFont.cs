namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
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

    internal class CompactFontFormatCidFont : CompactFontFormatFont
    {
        public IReadOnlyList<CompactFontFormatTopLevelDictionary> FontDictionaries { get; }
        public IReadOnlyList<CompactFontFormatPrivateDictionary> PrivateDictionaries { get; }
        public IReadOnlyList<CompactFontFormatIndex> LocalSubroutines { get; }
        public ICompactFontFormatFdSelect FdSelect { get; }

        public CompactFontFormatCidFont(CompactFontFormatTopLevelDictionary topDictionary, CompactFontFormatPrivateDictionary privateDictionary, 
            ICompactFontFormatCharset charset, 
            Union<Type1CharStrings, Type2CharStrings> charStrings,
            IReadOnlyList<CompactFontFormatTopLevelDictionary> fontDictionaries,
            IReadOnlyList<CompactFontFormatPrivateDictionary> privateDictionaries,
            IReadOnlyList<CompactFontFormatIndex> localSubroutines,
            ICompactFontFormatFdSelect fdSelect) : base(topDictionary, privateDictionary, charset, charStrings)
        {
            FontDictionaries = fontDictionaries;
            PrivateDictionaries = privateDictionaries;
            LocalSubroutines = localSubroutines;
            FdSelect = fdSelect;
        }
    }
}
