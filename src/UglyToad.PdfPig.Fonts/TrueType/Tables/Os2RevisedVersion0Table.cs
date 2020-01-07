namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System.Collections.Generic;
    using System.IO;
    using Core;

    /// <summary>
    /// Version 0 was defined in TrueType revision 1.5 and includes fields not in the Apple specification.
    /// </summary>
    public class Os2RevisedVersion0Table : Os2Table
    {
        /// <summary>
        /// Typographic ascender.
        /// </summary>
        public short TypographicAscender { get; }

        /// <summary>
        /// Typographic descender.
        /// </summary>
        public short TypographicDescender { get; }

        /// <summary>
        /// Typographic line gap.
        /// </summary>
        public short TypographicLineGap { get; }

        /// <summary>
        /// The Windows ascender metric. This should be used to specify the height above the baseline for a clipping region. 
        /// </summary>
        public ushort WindowsAscent { get; }

        /// <summary>
        /// The Windows descender metric. This should be used to specify the vertical extent below the baseline for a clipping region. 
        /// </summary>
        public ushort WindowsDescent { get; }

        /// <summary>
        /// Create a new <see cref="Os2RevisedVersion0Table"/>.
        /// </summary>
        public Os2RevisedVersion0Table(TrueTypeHeaderTable directoryTable, ushort version, short xAverageCharacterWidth, ushort weightClass,
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
            ushort windowsDescent) : base(directoryTable, version, xAverageCharacterWidth, weightClass,
            widthClass,
            typeFlags,
            ySubscriptXSize,
            ySubscriptYSize,
            ySubscriptXOffset,
            ySubscriptYOffset,
            ySuperscriptXSize,
            ySuperscriptYSize,
            ySuperscriptXOffset,
            ySuperscriptYOffset,
            yStrikeoutSize,
            yStrikeoutPosition,
            familyClass,
            panose,
            unicodeRanges,
            vendorId,
            fontSelectionFlags,
            firstCharacterIndex,
            lastCharacterIndex)
        {
            TypographicAscender = typographicAscender;
            TypographicDescender = typographicDescender;
            TypographicLineGap = typographicLineGap;
            WindowsAscent = windowsAscent;
            WindowsDescent = windowsDescent;
        }

        /// <inheritdoc />
        public override void Write(Stream stream)
        {
            base.Write(stream);
            stream.WriteShort(TypographicAscender);
            stream.WriteShort(TypographicDescender);
            stream.WriteShort(TypographicLineGap);
            stream.WriteUShort(WindowsAscent);
            stream.WriteUShort(WindowsDescent);
        }
    }
}