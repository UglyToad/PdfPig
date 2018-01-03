namespace UglyToad.Pdf.Fonts.TrueType.Tables
{
    using System;

    /// <summary>
    /// This table establishes the memory requirements for the font.
    /// </summary>
    internal class BasicMaximumProfileTable : ITable
    {
        public string Tag => TrueTypeHeaderTable.Maxp;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public bool IsCompressedFontFormat => Version == 0.5m;

        /// <summary>
        /// The table version number. CFF fonts must use version 0.5 and only set number of glyphs. TrueType must use version 1.
        /// </summary>
        public decimal Version { get; }

        /// <summary>
        /// The number of glyphs in the font.
        /// </summary>
        public int NumberOfGlyphs { get; }

        public BasicMaximumProfileTable(TrueTypeHeaderTable directoryTable, float version, int numberOfGlyphs)
        {
            DirectoryTable = directoryTable;
            Version = (decimal)version;
            NumberOfGlyphs = numberOfGlyphs;
        }

        public static BasicMaximumProfileTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table)
        {
            data.Seek(table.Offset);

            var version = data.Read32Fixed();
            var numberOfGlyphs = data.ReadUnsignedShort();
            if (Math.Abs(version - 0.5) < float.Epsilon)
            {
                return new BasicMaximumProfileTable(table, version, numberOfGlyphs);
            }

            var maxPoints = data.ReadUnsignedShort();
            var maxContours = data.ReadUnsignedShort();
            var maxCompositePoints = data.ReadUnsignedShort();
            var maxCompositeContours = data.ReadUnsignedShort();
            
            var maxZones = data.ReadUnsignedShort();
            var maxTwilightPoints = data.ReadUnsignedShort();
            var maxStorage = data.ReadUnsignedShort();
            var maxFunctionDefs = data.ReadUnsignedShort();
            var maxInstructionDefs = data.ReadUnsignedShort();
            var maxStackElements = data.ReadUnsignedShort();
            var maxSizeOfInstructions = data.ReadUnsignedShort();
            var maxComponentElements = data.ReadUnsignedShort();
            var maxComponentDepth = data.ReadUnsignedShort();

            return new MaximumProfileTable(table, version, numberOfGlyphs, maxPoints,
                maxContours, maxCompositePoints, maxCompositeContours, maxZones,
                maxTwilightPoints, maxStorage, maxFunctionDefs, maxInstructionDefs,
                maxStackElements, maxSizeOfInstructions, maxComponentElements,
                maxComponentDepth);
        }
    }

    internal class MaximumProfileTable : BasicMaximumProfileTable
    {
        /// <summary>
        /// Maximum number of points in a non-composite glyph.
        /// </summary>
        public int MaximumPoints { get; }

        /// <summary>
        /// Maximum number of contours in a non-composite glyph.
        /// </summary>
        public int MaximumContours { get; }

        /// <summary>
        /// Maximum number of points in a composite glyph.
        /// </summary>
        public int MaximumCompositePoints { get; }

        /// <summary>
        /// Maximum number of contours in a composite glyph.
        /// </summary>
        public int MaximumCompositeContours { get; }

        /// <summary>
        /// 1 if instructions do not use the twilight zone (Z0). 2 if they do. Usually 2.
        /// </summary>
        public int MaximumZones { get; }

        /// <summary>
        /// Maximum number of points to use in Z0 (twilight zone).
        /// </summary>
        public int MaximumTwilightPoints { get; }

        /// <summary>
        /// Maximum number of storage area locations.
        /// </summary>
        public int MaximumStorage { get; }

        /// <summary>
        /// Maximum number of function definitions.
        /// </summary>
        public int MaximumFunctionDefinitions { get; }

        /// <summary>
        /// Maximum number of instruction definitions.
        /// </summary>
        public int MaximumInstructionDefinitions { get; }

        /// <summary>
        /// Maximum stack depth.
        /// </summary>
        public int MaximumStackElements { get; }

        /// <summary>
        /// Maximum byte count for glpyh instructions
        /// </summary>
        public int MaximumSizeOfInstructions { get; }

        /// <summary>
        /// Maximum number of components at the top level for a composite glyph.
        /// </summary>
        public int MaximumComponentElements { get; }

        /// <summary>
        /// Maximum level of recursion. 1 for simple components.
        /// </summary>
        public int MaximumComponentDepth { get; }

        public MaximumProfileTable(TrueTypeHeaderTable directoryTable, float version, int numberOfGlyphs, int maximumPoints, int maximumContours, int maximumCompositePoints, int maximumCompositeContours, int maximumZones, int maximumTwilightPoints, int maximumStorage, int maximumFunctionDefinitions, int maximumInstructionDefinitions, int maximumStackElements, int maximumSizeOfInstructions, int maximumComponentElements, int maximumComponentDepth) : base(directoryTable, version, numberOfGlyphs)
        {
            MaximumPoints = maximumPoints;
            MaximumContours = maximumContours;
            MaximumCompositePoints = maximumCompositePoints;
            MaximumCompositeContours = maximumCompositeContours;
            MaximumZones = maximumZones;
            MaximumTwilightPoints = maximumTwilightPoints;
            MaximumStorage = maximumStorage;
            MaximumFunctionDefinitions = maximumFunctionDefinitions;
            MaximumInstructionDefinitions = maximumInstructionDefinitions;
            MaximumStackElements = maximumStackElements;
            MaximumSizeOfInstructions = maximumSizeOfInstructions;
            MaximumComponentElements = maximumComponentElements;
            MaximumComponentDepth = maximumComponentDepth;
        }
    }
}
