namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using Charsets;
    using CharStrings;
    using Dictionaries;
    using Encodings;
    using Geometry;
    using Type1.CharStrings;
    using Util;

    internal class CompactFontFormatFont
    {
        public CompactFontFormatTopLevelDictionary TopDictionary { get; }
        public CompactFontFormatPrivateDictionary PrivateDictionary { get; }
        public ICompactFontFormatCharset Charset { get; }
        public Union<Type1CharStrings, Type2CharStrings> CharStrings { get; }
        public Encoding Encoding { get; }

        public CompactFontFormatFont(CompactFontFormatTopLevelDictionary topDictionary, CompactFontFormatPrivateDictionary privateDictionary,
            ICompactFontFormatCharset charset,
            Union<Type1CharStrings, Type2CharStrings> charStrings, Encoding fontEncoding)
        {
            TopDictionary = topDictionary;
            PrivateDictionary = privateDictionary;
            Charset = charset;
            CharStrings = charStrings;
            Encoding = fontEncoding;
        }

        public PdfRectangle? GetCharacterBoundingBox(string characterName)
        {
            var defaultWidthX = GetDefaultWidthX(characterName);
            var nominalWidthX = GetNominalWidthX(characterName);

            var result = CharStrings.Match(x => throw new NotImplementedException("Type 1 CharStrings in a CFF font are currently unsupported."),
                x =>
                {
                    var glyph = x.Generate(characterName, defaultWidthX, nominalWidthX);
                    var rectangle = glyph.Path.GetBoundingRectangle();
                    if (rectangle.HasValue)
                    {
                        return rectangle;
                    }

                    var defaultBoundingBox = TopDictionary.FontBoundingBox;
                    return new PdfRectangle(0, 0, glyph.Width.GetValueOrDefault(), defaultBoundingBox.Height);
                });

            return result;
        }

        protected virtual decimal GetDefaultWidthX(string characterName)
        {
            return PrivateDictionary.DefaultWidthX;
        }

        protected virtual decimal GetNominalWidthX(string characterName)
        {
            return PrivateDictionary.NominalWidthX;
        }
    }

    internal class CompactFontFormatCidFont : CompactFontFormatFont
    {
        public IReadOnlyList<CompactFontFormatTopLevelDictionary> FontDictionaries { get; }
        public IReadOnlyList<CompactFontFormatPrivateDictionary> PrivateDictionaries { get; }
        public ICompactFontFormatFdSelect FdSelect { get; }

        public CompactFontFormatCidFont(CompactFontFormatTopLevelDictionary topDictionary, CompactFontFormatPrivateDictionary privateDictionary, 
            ICompactFontFormatCharset charset, 
            Union<Type1CharStrings, Type2CharStrings> charStrings,
            IReadOnlyList<CompactFontFormatTopLevelDictionary> fontDictionaries,
            IReadOnlyList<CompactFontFormatPrivateDictionary> privateDictionaries,
            ICompactFontFormatFdSelect fdSelect) : base(topDictionary, privateDictionary, charset, charStrings, null)
        {
            FontDictionaries = fontDictionaries;
            PrivateDictionaries = privateDictionaries;
            FdSelect = fdSelect;
        }

        protected override decimal GetDefaultWidthX(string characterName)
        {
            if (!TryGetPrivateDictionaryForCharacter(characterName, out var dictionary))
            {
                return 1000;
            }

            return dictionary.DefaultWidthX;
        }

        protected override decimal GetNominalWidthX(string characterName)
        {
            if (!TryGetPrivateDictionaryForCharacter(characterName, out var dictionary))
            {
                return 0;
            }

            return dictionary.NominalWidthX;
        }

        private bool TryGetPrivateDictionaryForCharacter(string characterName, out CompactFontFormatPrivateDictionary dictionary)
        {
            dictionary = null;

            var glyphId = Charset.GetGlyphIdByName(characterName);

            var fd = FdSelect.GetFontDictionaryIndex(glyphId);
            if (fd == -1)
            {
                return false;
            }

            dictionary = PrivateDictionaries[fd];

            return true;
        }
    }
}
