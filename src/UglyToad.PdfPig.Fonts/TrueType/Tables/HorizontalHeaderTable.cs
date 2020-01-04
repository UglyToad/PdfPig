namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    /// <summary>
    /// The 'hhea' table contains information needed to layout fonts whose characters are written horizontally, that is, either left to right or right to left. 
    /// This table contains information that is general to the font as a whole.
    /// </summary>
    internal class HorizontalHeaderTable : ITrueTypeTable
    {
        public string Tag => TrueTypeHeaderTable.Hhea;

        public TrueTypeHeaderTable DirectoryTable { get; }

        /// <summary>
        /// Major version number of this table (1).
        /// </summary>
        public int MajorVersion { get; }

        /// <summary>
        /// Minor version number of this table (0).
        /// </summary>
        public int MinorVersion { get; }

        /// <summary>
        /// Distance from baseline to highest ascender.
        /// </summary>
        public short Ascent { get; }

        /// <summary>
        /// Distance from baseline to lower descender.
        /// </summary>
        public short Descent { get; }

        /// <summary>
        /// The typographic line gap.
        /// </summary>
        public short LineGap { get; }

        /// <summary>
        /// The maximum advance width value as given by the Horizontal Metrics table.
        /// </summary>
        public ushort AdvanceWidthMaximum { get; }

        /// <summary>
        /// The minimum left side bearing as given by the Horizontal Metrics table.
        /// </summary>
        public short MinimumLeftSideBearing { get; }

        /// <summary>
        /// The minimum right sidebearing.
        /// </summary>
        public short MinimumRightSideBearing { get; }

        /// <summary>
        /// The maximum X extent.
        /// </summary>
        public short XMaxExtent { get; }

        /// <summary>
        /// Used to calculate the slope of the cursor. 1 is vertical.
        /// </summary>
        public short CaretSlopeRise { get; }

        /// <summary>
        /// 0 is vertical.
        /// </summary>
        public short CaretSlopeRun { get; }

        /// <summary>
        /// The amount by which a slanted highlight on a glyph should be shifted to provide the best appearance. 0 for non-slanted fonts.
        /// </summary>
        public short CaretOffset { get; }

        /// <summary>
        /// 0 for the current format.
        /// </summary>
        public short MetricDataFormat { get; }

        /// <summary>
        /// Number of horizontal metrics in the Horizontal Metrics table.
        /// </summary>
        public ushort NumberOfHeaderMetrics { get; }

        public HorizontalHeaderTable(TrueTypeHeaderTable directoryTable, int majorVersion, int minorVersion, short ascent, short descent, 
            short lineGap, ushort advanceWidthMaximum, 
            short minimumLeftSideBearing, short minimumRightSideBearing,
            short xMaxExtent, short caretSlopeRise, 
            short caretSlopeRun, short caretOffset, 
            short metricDataFormat, ushort numberOfHeaderMetrics)
        {
            DirectoryTable = directoryTable;
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            Ascent = ascent;
            Descent = descent;
            LineGap = lineGap;
            AdvanceWidthMaximum = advanceWidthMaximum;
            MinimumLeftSideBearing = minimumLeftSideBearing;
            MinimumRightSideBearing = minimumRightSideBearing;
            XMaxExtent = xMaxExtent;
            CaretSlopeRise = caretSlopeRise;
            CaretSlopeRun = caretSlopeRun;
            CaretOffset = caretOffset;
            MetricDataFormat = metricDataFormat;
            NumberOfHeaderMetrics = numberOfHeaderMetrics;
        }
    }
}
