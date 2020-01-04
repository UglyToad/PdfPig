namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using Core;

    /// <inheritdoc />
    /// <summary>
    /// The 'head' table contains global information about the font. 
    /// It contains things like as the font version number, the creation and modification dates, revision number and basic typographic data that applies to the font as a whole.
    /// </summary>
    public class HeaderTable : ITrueTypeTable
    {
        /// <inheritdoc />
        public string Tag => TrueTypeHeaderTable.Head;

        /// <inheritdoc />
        public TrueTypeHeaderTable DirectoryTable { get; }

        /// <summary>
        /// Version number.
        /// </summary>
        public float Version { get; }

        /// <summary>
        /// Revision.
        /// </summary>
        public float Revision { get; }

        /// <summary>
        /// Checksum adjustment is used to derive the checksum of the entire TrueType file.
        /// </summary>
        public uint CheckSumAdjustment { get; }

        /// <summary>
        /// 0x5F0F3CF5.
        /// </summary>
        public uint MagicNumber { get; }

        /// <summary>
        /// Flags.
        /// </summary>
        public ushort Flags { get; }

        /// <summary>
        /// Units per em.
        /// </summary>
        public ushort UnitsPerEm { get; }

        /// <summary>
        /// Created date.
        /// </summary>
        public DateTime Created { get; }

        /// <summary>
        /// Modified date.
        /// </summary>
        public DateTime Modified { get; }

        /// <summary>
        /// Minimum rectangle which contains all glyphs.
        /// </summary>
        public PdfRectangle Bounds { get; }

        /// <summary>
        /// MacStyle flags.
        /// </summary>
        public HeaderMacStyle MacStyle { get; }

        /// <summary>
        /// Smallest readable size in pixels.
        /// </summary>
        public ushort LowestRecommendedPpem { get; }

        /// <summary>
        /// Font direction hint.
        /// </summary>
        public FontDirection FontDirectionHint { get; }

        /// <summary>
        /// 0 for short offsets, 1 for long.
        /// </summary>
        public IndexToLocationTable.EntryFormat IndexToLocFormat { get; }

        /// <summary>
        /// 0 for current format.
        /// </summary>
        public short GlyphDataFormat { get; }

        /// <summary>
        /// Create a new <see cref="HeaderTable"/>.
        /// </summary>
        public HeaderTable(TrueTypeHeaderTable directoryTable, float version, float revision, uint checkSumAdjustment, 
            uint magicNumber, ushort flags, ushort unitsPerEm, 
            DateTime created, DateTime modified, 
            short xMin, short yMin, 
            short xMax, short yMax, 
            ushort macStyle, 
            ushort lowestRecommendedPpem, 
            short fontDirectionHint,
            IndexToLocationTable.EntryFormat indexToLocFormat, 
            short glyphDataFormat)
        {
            DirectoryTable = directoryTable;
            Version = version;
            Revision = revision;
            CheckSumAdjustment = checkSumAdjustment;
            MagicNumber = magicNumber;
            Flags = flags;
            UnitsPerEm = unitsPerEm;
            Created = created;
            Modified = modified;
            Bounds = new PdfRectangle(xMin, yMin, xMax, yMax);
            MacStyle = (HeaderMacStyle)macStyle;
            LowestRecommendedPpem = lowestRecommendedPpem;
            FontDirectionHint = (FontDirection)fontDirectionHint;
            IndexToLocFormat = indexToLocFormat;
            GlyphDataFormat = glyphDataFormat;
        }

        /// <summary>
        /// Read the header table from the data stream.
        /// </summary>
        public static HeaderTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table)
        {
            data.Seek(table.Offset);
            var version = data.Read32Fixed();
            var fontRevision = data.Read32Fixed();
            var checkSumAdjustment = data.ReadUnsignedInt();
            var magicNumber = data.ReadUnsignedInt();

            if (magicNumber != 0x5F0F3CF5)
            {
                throw new InvalidOperationException("The magic number for this TrueType font was incorrect. Value was: " + magicNumber);
            }

            var flags = data.ReadUnsignedShort();
            var unitsPerEm = data.ReadUnsignedShort();

            if (unitsPerEm < 16 || unitsPerEm > 16384)
            {
                throw new InvalidOperationException($"The units per em for this TrueType font was incorrect, value should be between 16 and 16384 but found {unitsPerEm} istead.");
            }

            DateTime created;
            try
            {
                created = data.ReadInternationalDate();
            }
            catch (InvalidFontFormatException)
            {
                created = DateTime.MinValue;
            }

            DateTime modified;
            try
            {
                modified = data.ReadInternationalDate();
            }
            catch (InvalidFontFormatException)
            {
                modified = DateTime.MinValue;
            }

            var xMin = data.ReadSignedShort();
            var yMin = data.ReadSignedShort();
            var xMax = data.ReadSignedShort();
            var yMax = data.ReadSignedShort();
            var macStyle = data.ReadUnsignedShort();
            var lowestRecPpem = data.ReadUnsignedShort();
            var fontDirectionHint = data.ReadSignedShort();
            var indexToLocFormat = (IndexToLocationTable.EntryFormat)data.ReadSignedShort();
            var glyphDataFormat = data.ReadSignedShort();

            return new HeaderTable(table, version, fontRevision, checkSumAdjustment,
                magicNumber, flags, unitsPerEm, created, modified,
                xMin, yMin, xMax, yMax, macStyle, lowestRecPpem,
                fontDirectionHint, indexToLocFormat, glyphDataFormat);
        }

        /// <summary>
        /// Values of the font direction hint.
        /// </summary>
        public enum FontDirection : short
        {
            /// <summary>
            /// Strongly right to left with neutrals.
            /// </summary>
            StronglyRightToLeftWithNeutrals = -2,
            /// <summary>
            /// Strongly right to left.
            /// </summary>
            StronglyRightToLeft = -1,
            /// <summary>
            /// Full mixed directional glyphs.
            /// </summary>
            FullyMixedDirectional = 0,
            /// <summary>
            /// Strongly left to right.
            /// </summary>
            StronglyLeftToRight = 1,
            /// <summary>
            /// Strongly left to right with neutrals.
            /// </summary>
            StronglyLeftToRightWithNeutrals = 2
        }

        /// <summary>
        /// Values of the Mac Style flag in the header table.
        /// </summary>
        [Flags]
        public enum HeaderMacStyle : ushort
        {
            /// <summary>
            /// Bold.
            /// </summary>
            Bold = 1 << 0,
            /// <summary>
            /// Italic.
            /// </summary>
            Italic = 1 << 1,
            /// <summary>
            /// Underline.
            /// </summary>
            Underline = 1 << 2,
            /// <summary>
            /// Outline.
            /// </summary>
            Outline = 1 << 3,
            /// <summary>
            /// Shadow.
            /// </summary>
            Shadow = 1 << 4,
            /// <summary>
            /// Condensed (narrow).
            /// </summary>
            Condensed = 1 << 5,
            /// <summary>
            /// Extended.
            /// </summary>
            Extended = 1 << 6
        }
    }
}
