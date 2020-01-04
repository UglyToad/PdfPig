namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core;

    /// <summary>
    /// The most basic format of the OS/2 table, excluding the fields not included in the Apple version of the specification.
    /// </summary>
    internal class Os2Table : ITrueTypeTable, IWriteable
    {
        public string Tag => TrueTypeHeaderTable.Os2;

        public TrueTypeHeaderTable DirectoryTable { get; }

        /// <summary>
        /// The version number 0 - 5 detailing the layout of the OS/2 table.
        /// </summary>
        public ushort Version { get; }

        /// <summary>
        /// The average width of all non-zero width characters in the font.
        /// </summary>
        public short XAverageCharacterWidth { get; }

        /// <summary>
        /// Indicates the visual weight of characters in the font from 1 - 1000.
        /// </summary>
        public ushort WeightClass { get; }

        /// <summary>
        /// The percentage difference from normal of the aspect ratio for this font.
        /// </summary>
        public ushort WidthClass { get; }

        /// <summary>
        /// The font embedding licensing rights for this font.
        /// </summary>
        public ushort TypeFlags { get; }

        /// <summary>
        /// The recommended horizontal size for subscripts using this font.
        /// </summary>
        public short YSubscriptXSize { get; }

        /// <summary>
        /// The recommended vertical size for subscripts using this font.
        /// </summary>
        public short YSubscriptYSize { get; }

        /// <summary>
        /// The recommended horizontal offset (from the previous glyph origin to the subscript's origin) for subscripts using this font.
        /// </summary>
        public short YSubscriptXOffset { get; }

        /// <summary>
        /// The recommended vertical offset (from the previous glyph origin to the subscript's origin) for subscripts using this font.
        /// </summary>
        public short YSubscriptYOffset { get; }

        /// <summary>
        /// The recommended horizontal size for superscripts using this font.
        /// </summary>
        public short YSuperscriptXSize { get; }

        /// <summary>
        /// The recommended vertical size for superscripts using this font.
        /// </summary>
        public short YSuperscriptYSize { get; }

        /// <summary>
        /// The recommended horizontal offset (from the previous glyph origin to the superscript's origin) for superscripts using this font.
        /// </summary>
        public short YSuperscriptXOffset { get; }

        /// <summary>
        /// The recommended vertical offset (from the previous glyph origin to the superscript's origin) for superscripts using this font.
        /// </summary>
        public short YSuperscriptYOffset { get; }

        /// <summary>
        /// Thickness of the strikeout stroke.
        /// </summary>
        public short YStrikeoutSize { get; }

        /// <summary>
        /// Position of the top of the strikeout stroke relative to the baseline.
        /// Positive values being above the baseline, negative values below.
        /// </summary>
        public short YStrikeoutPosition { get; }

        /// <summary>
        /// Value registered by IBM for each font family to find substitutes.
        /// The high byte is the family class, the low byte is the family subclass.
        /// </summary>
        public short FamilyClass { get; }

        /// <summary>
        /// The PANOSE definition of 10 bytes defines various information about the
        /// font enabling matching fonts based on requirements. The meaning of each
        /// byte in the PANOSE definition depends on the preceding bytes. The first byte
        /// is the family type, Latin, Latin Hand Written, etc.
        /// </summary>
        public IReadOnlyList<byte> Panose { get; }

        /// <summary>
        /// Specifies Unicode blocks supported by the font file for the Microsoft platform.
        /// </summary>
        public IReadOnlyList<uint> UnicodeRanges { get; }

        /// <summary>
        /// The four-character identifier for the vendor of the given type face.
        /// </summary>
        public string VendorId { get; }

        /// <summary>
        /// Contains information concerning the nature of the font patterns.
        /// </summary>
        public ushort FontSelectionFlags { get; }

        /// <summary>
        /// The minimum Unicode character code in this font.
        /// </summary>
        public ushort FirstCharacterIndex { get; }

        /// <summary>
        /// The maximum Unicode character code in this font.
        /// </summary>
        public ushort LastCharacterIndex { get; }

        /// <summary>
        /// Create a new <see cref="Os2Table"/>.
        /// </summary>
        public Os2Table(TrueTypeHeaderTable directoryTable, ushort version, short xAverageCharacterWidth, ushort weightClass, 
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
            ushort lastCharacterIndex)
        {
            DirectoryTable = directoryTable;
            Version = version;
            XAverageCharacterWidth = xAverageCharacterWidth;
            WeightClass = weightClass;
            WidthClass = widthClass;
            TypeFlags = typeFlags;
            YSubscriptXSize = ySubscriptXSize;
            YSubscriptYSize = ySubscriptYSize;
            YSubscriptXOffset = ySubscriptXOffset;
            YSubscriptYOffset = ySubscriptYOffset;
            YSuperscriptXSize = ySuperscriptXSize;
            YSuperscriptYSize = ySuperscriptYSize;
            YSuperscriptXOffset = ySuperscriptXOffset;
            YSuperscriptYOffset = ySuperscriptYOffset;
            YStrikeoutSize = yStrikeoutSize;
            YStrikeoutPosition = yStrikeoutPosition;
            FamilyClass = familyClass;
            Panose = panose;
            UnicodeRanges = unicodeRanges;
            VendorId = vendorId;
            FontSelectionFlags = fontSelectionFlags;
            FirstCharacterIndex = firstCharacterIndex;
            LastCharacterIndex = lastCharacterIndex;
        }

        public virtual void Write(Stream stream)
        {
            stream.WriteUShort(Version);
            stream.WriteShort(XAverageCharacterWidth);
            stream.WriteUShort(WeightClass);
            stream.WriteUShort(WidthClass);

            stream.WriteShort(TypeFlags);

            stream.WriteShort(YSubscriptXSize);
            stream.WriteShort(YSubscriptYSize);
            stream.WriteShort(YSubscriptXOffset);
            stream.WriteShort(YSubscriptYOffset);

            stream.WriteShort(YSuperscriptXSize);
            stream.WriteShort(YSuperscriptYSize);
            stream.WriteShort(YSuperscriptXOffset);
            stream.WriteShort(YSuperscriptYOffset);

            stream.WriteShort(YStrikeoutSize);
            stream.WriteShort(YStrikeoutPosition);

            stream.WriteShort(FamilyClass);

            stream.Write(Panose.ToArray(), 0, Panose.Count);

            for (var i = 0; i < UnicodeRanges.Count; i++)
            {
                stream.WriteUInt(UnicodeRanges[i]);
            }

            for (var i = 0; i < VendorId.Length; i++)
            {
                stream.WriteByte((byte)VendorId[i]);
            }

            stream.WriteUShort(FontSelectionFlags);
            stream.WriteUShort(FirstCharacterIndex);
            stream.WriteUShort(LastCharacterIndex);
        }
    }
}
