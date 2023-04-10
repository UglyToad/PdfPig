﻿namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using Charsets;
    using CharStrings;
    using Core;
    using Dictionaries;
    using Encodings;
    using System;
    using System.Collections.Generic;
    using Type1.CharStrings;

    /// <summary>
    /// A Compact Font Format (CFF) font.
    /// </summary>
    public class CompactFontFormatFont
    {
        internal CompactFontFormatTopLevelDictionary TopDictionary { get; }
        internal CompactFontFormatPrivateDictionary PrivateDictionary { get; }
        internal ICompactFontFormatCharset Charset { get; }
        internal Union<Type1CharStrings, Type2CharStrings> CharStrings { get; }

        /// <summary>
        /// The encoding for this font.
        /// </summary>
        public Encoding Encoding { get; }

        /// <summary>
        /// The font matrix for this font.
        /// </summary>
        public TransformationMatrix FontMatrix => TopDictionary.FontMatrix;

        /// <summary>
        /// The value of Weight from the top dictionary or <see langword="null"/>.
        /// </summary>
        public string Weight => TopDictionary?.Weight;

        /// <summary>
        /// The value of Italic Angle from the top dictionary or 0.
        /// </summary>
        public decimal ItalicAngle => TopDictionary?.ItalicAngle ?? 0;

        internal CompactFontFormatFont(CompactFontFormatTopLevelDictionary topDictionary, CompactFontFormatPrivateDictionary privateDictionary,
            ICompactFontFormatCharset charset,
            Union<Type1CharStrings, Type2CharStrings> charStrings, Encoding fontEncoding)
        {
            TopDictionary = topDictionary;
            PrivateDictionary = privateDictionary;
            Charset = charset;
            CharStrings = charStrings;
            Encoding = fontEncoding;
        }

        /// <summary>
        /// Get the bounding box for the character with the given name.
        /// </summary>
        public PdfRectangle? GetCharacterBoundingBox(string characterName)
        {
            var defaultWidthX = GetDefaultWidthX(characterName);
            var nominalWidthX = GetNominalWidthX(characterName);

            if (CharStrings.TryGetFirst(out var _))
            {
                throw new NotImplementedException("Type 1 CharStrings in a CFF font are currently unsupported.");
            }

            if (!CharStrings.TryGetSecond(out var type2CharStrings))
            {
                return null;
            }

            var glyph = type2CharStrings.Generate(characterName, (double)defaultWidthX, (double)nominalWidthX);
            var rectangle = PdfSubpath.GetBoundingRectangle(glyph.Path);
            if (rectangle.HasValue)
            {
                return rectangle;
            }

            var defaultBoundingBox = TopDictionary.FontBoundingBox;
            return new PdfRectangle(0, 0, glyph.Width.GetValueOrDefault(), defaultBoundingBox.Height);
        }

        /// <summary>
        /// Get the pdfpath for the character with the given name.
        /// </summary>
        /// <param name="characterName"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool TryGetPath(string characterName, out IReadOnlyList<PdfSubpath> path)
        {
            var defaultWidthX = GetDefaultWidthX(characterName);
            var nominalWidthX = GetNominalWidthX(characterName);

            if (CharStrings.TryGetFirst(out var _))
            {
                throw new NotImplementedException("Type 1 CharStrings in a CFF font are currently unsupported.");
            }

            if (!CharStrings.TryGetSecond(out var type2CharStrings))
            {
                path = null;
                return false;
            }

            path = type2CharStrings.Generate(characterName, (double)defaultWidthX, (double)nominalWidthX).Path;
            return true;
        }

        /// <summary>
        /// GetCharacterPath
        /// </summary>
        /// <param name="characterName"></param>
        /// <returns></returns>
        public IReadOnlyList<PdfSubpath> GetCharacterPath(string characterName)
        {
            var defaultWidthX = GetDefaultWidthX(characterName);
            var nominalWidthX = GetNominalWidthX(characterName);

            if (CharStrings.TryGetFirst(out var _))
            {
                throw new NotImplementedException("Type 1 CharStrings in a CFF font are currently unsupported.");
            }

            if (!CharStrings.TryGetSecond(out var type2CharStrings))
            {
                return null;
            }

            return type2CharStrings.Generate(characterName, (double)defaultWidthX, (double)nominalWidthX).Path;
        }

        /// <summary>
        /// Get the default width of x for the character.
        /// </summary>
        protected virtual decimal GetDefaultWidthX(string characterName)
        {
            return PrivateDictionary.DefaultWidthX;
        }

        /// <summary>
        /// Get the nominal width of x for the character.
        /// </summary>
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
