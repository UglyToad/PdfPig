namespace UglyToad.Pdf.Fonts.TrueType.Tables
{
    using System;

    /// <summary>
    /// Gives global information about the font.
    /// </summary>
    internal class HeaderTable : ITable
    {
        public string Tag => TrueTypeFontTable.Head;
        
        /// <summary>
        /// Bold macStyle flag.
        /// </summary>
        public const int MacStyleBold = 1;
        
        /// <summary>
        /// Italic macStyle flag.
        /// </summary>
        public const int MacStyleItalic = 2;

        public TrueTypeFontTable DirectoryTable { get; }

        public decimal Version { get; }

        public decimal Revision { get; }

        public long CheckSumAdjustment { get; }

        public long MagicNumber { get; }

        public int Flags { get; }

        public int UnitsPerEm { get; }

        public DateTime Created { get; }

        public DateTime Modified { get; }

        public short XMin { get; }

        public short YMin { get; }

        public short XMax { get; }

        public short YMax { get; }

        public int MacStyle { get; }

        /// <summary>
        /// Smallest readable size in pixels.
        /// </summary>
        public int LowestRecommendedPpem { get; }

        public FontDirection FontDirectionHint { get; }

        /// <summary>
        /// 0 for short offsets, 1 for long.
        /// </summary>
        public short IndexToLocFormat { get; }

        /// <summary>
        /// 0 for current format.
        /// </summary>
        public short GlyphDataFormat { get; }

        public HeaderTable(TrueTypeFontTable directoryTable, decimal version, decimal revision, long checkSumAdjustment, 
            long magicNumber, int flags, int unitsPerEm, 
            DateTime created, DateTime modified, 
            short xMin, short yMin, 
            short xMax, short yMax, 
            int macStyle, 
            int lowestRecommendedPpem, 
            short fontDirectionHint, 
            short indexToLocFormat, 
            short glyphDataFormat)
        {
            DirectoryTable = directoryTable ?? throw new ArgumentNullException(nameof(directoryTable));
            Version = version;
            Revision = revision;
            CheckSumAdjustment = checkSumAdjustment;
            MagicNumber = magicNumber;
            Flags = flags;
            UnitsPerEm = unitsPerEm;
            Created = created;
            Modified = modified;
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
            MacStyle = macStyle;
            LowestRecommendedPpem = lowestRecommendedPpem;
            FontDirectionHint = (FontDirection)fontDirectionHint;
            IndexToLocFormat = indexToLocFormat;
            GlyphDataFormat = glyphDataFormat;
        }

        public enum FontDirection
        {
            StronglyRightToLeftWithNeutrals = -2,
            StronglyRightToLeft = -1,
            FullyMixedDirectional = 0,
            StronglyLeftToRight = 1,
            StronglyLeftToRightWithNeutrals = 2
        }
    }
}
