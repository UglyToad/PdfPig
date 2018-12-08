namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System.Collections.Generic;

    /// <summary>
    /// Version 5 was defined in OpenType 1.7. 
    /// Version 5 has two additional fields beyond those in versions 2 - 4.
    /// </summary>
    internal class Os2Version5OpenTypeTable : Os2Version2To4OpenTypeTable
    {
        /// <summary>
        /// This value is the lower value of the size range for which this font has been designed. 
        /// The units for this field are TWIPs (one-twentieth of a point, or 1440 per inch). 
        /// This is the inclusive lower bound.
        /// </summary>
        public ushort LowerOpticalPointSize { get; }

        /// <summary>
        /// This value is the upper value of the size range for which this font has been designed. 
        /// The units for this field are TWIPs (one-twentieth of a point, or 1440 per inch). 
        /// This is the exclusive upper bound.
        /// </summary>
        public ushort UpperOpticalPointSize { get; }

        /// <summary>
        /// Create a new <see cref="Os2Version5OpenTypeTable"/>.
        /// </summary>
        public Os2Version5OpenTypeTable(TrueTypeHeaderTable directoryTable, 
            ushort version, short xAverageCharacterWidth,
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
            ushort maximumContext,
            ushort lowerOpticalPointSize, 
            ushort upperOpticalPointSize) : base(directoryTable, version, xAverageCharacterWidth, weightClass, 
            widthClass, typeFlags, ySubscriptXSize, ySubscriptYSize, ySubscriptXOffset, ySubscriptYOffset,
            ySuperscriptXSize, ySuperscriptYSize, ySuperscriptXOffset, ySuperscriptYOffset, yStrikeoutSize,
            yStrikeoutPosition, familyClass, panose, unicodeRanges, vendorId, fontSelectionFlags,
            firstCharacterIndex, lastCharacterIndex, typographicAscender, typographicDescender,
            typographicLineGap, windowsAscent, windowsDescent, codePage1, codePage2, xHeight, 
            capHeight, defaultCharacter, breakCharacter, maximumContext)
        {
            LowerOpticalPointSize = lowerOpticalPointSize;
            UpperOpticalPointSize = upperOpticalPointSize;
        }
    }
}