namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System.Collections.Generic;

    /// <summary>
    /// Version 4 was defined in OpenType 1.5. Version 4 has the same fields as in version 2 and version 3. 
    /// Although new fields were not added beyond those in version 2 and 3, the specification of certain fields was revised. 
    /// </summary>
    internal class Os2Version2To4OpenTypeTable : Os2Version1Table
    {

        /// <summary>
        /// This metric specifies the distance between the baseline and the approximate height of non-ascending lowercase letters.
        /// </summary>
        public short XHeight { get; }

        /// <summary>
        /// This metric specifies the distance between the baseline and the approximate height of uppercase letters.
        /// </summary>
        public short CapHeight { get; }

        /// <summary>
        /// This is the Unicode code point, in UTF-16 encoding, of a character that can be used for a default glyph if a requested character is not supported. 
        /// If the value of this field is zero, glyph Id 0 is to be used for the default character.
        /// </summary>
        public ushort DefaultCharacter { get; }

        /// <summary>
        /// This is the Unicode code point, in UTF-16 encoding, of a character that can be used as a default break character. 
        /// The break character is used to separate words and justify text. 
        /// Most fonts specify U+0020 SPACE as the break character.
        /// </summary>
        public ushort BreakCharacter { get; }

        /// <summary>
        /// The maximum distance in glyphs that any feature of this font is capable of effecting. For example
        /// kerning has a value of 2 (1 for each glyph in the kerning pair). 
        /// Fonts with the 'f f i' ligature would have a value of 3.
        /// </summary>
        public ushort MaximumContext { get; }

        /// <summary>
        /// Create a new <see cref="Os2Version2To4OpenTypeTable"/>.
        /// </summary>
        public Os2Version2To4OpenTypeTable(TrueTypeHeaderTable directoryTable, ushort version, 
            short xAverageCharacterWidth, 
            ushort weightClass, 
            ushort widthClass, 
            ushort typeFlags, 
            short ySubscriptXSize, 
            short ySubscriptYSize, 
            short ySubscriptXOffset,
            short ySubscriptYOffset, 
            short ySuperscriptXSize, 
            short ySuperscriptYSize, 
            short ySuperscriptXOffset,
            short ySuperscriptYOffset, 
            short yStrikeoutSize,
            short yStrikeoutPosition, 
            short familyClass, 
            IReadOnlyList<byte> panose,
            IReadOnlyList<uint> unicodeRanges, 
            string vendorId, 
            ushort fontSelectionFlags,
            ushort firstCharacterIndex, 
            ushort lastCharacterIndex, 
            short typographicAscender,
            short typographicDescender, 
            short typographicLineGap, 
            ushort windowsAscent,
            ushort windowsDescent, 
            uint codePage1,
            uint codePage2, 
            short xHeight,
            short capHeight, 
            ushort defaultCharacter,
            ushort breakCharacter,
            ushort maximumContext) : base(directoryTable, version, xAverageCharacterWidth, weightClass, widthClass, 
            typeFlags, ySubscriptXSize, ySubscriptYSize, ySubscriptXOffset, ySubscriptYOffset, ySuperscriptXSize,
            ySuperscriptYSize, ySuperscriptXOffset, ySuperscriptYOffset, yStrikeoutSize, yStrikeoutPosition,
            familyClass, panose, unicodeRanges, vendorId, fontSelectionFlags, firstCharacterIndex, lastCharacterIndex,
            typographicAscender, typographicDescender, typographicLineGap, windowsAscent, windowsDescent, 
            codePage1, codePage2)
        {
            XHeight = xHeight;
            CapHeight = capHeight;
            DefaultCharacter = defaultCharacter;
            BreakCharacter = breakCharacter;
            MaximumContext = maximumContext;
        }
    }
}