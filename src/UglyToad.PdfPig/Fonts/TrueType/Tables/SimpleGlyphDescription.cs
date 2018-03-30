namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using Geometry;

    internal class SimpleGlyphDescription : IGlyphDescription
    {
        /// <summary>
        /// The bounding rectangle for the character.
        /// </summary>
        public PdfRectangle GlyphBounds { get; }

        /// <summary>
        /// The total number of bytes for instructions.
        /// </summary>
        public int InstructionLength { get; }
        
        /// <summary>
        /// An array of the last points of each contour.
        /// </summary>
        public int[] EndPointsOfContours { get; }

        /// <summary>
        /// Array of flags for each coordinate in the outline.
        /// </summary>
        public SimpleGlyphFlags[] Flags { get; }

        /// <summary>
        /// The x-coordinates of the points in this glyph. The first coordinates are relative to the origin (0, 0)
        /// the rest are relative to the previous point.
        /// </summary>
        public short[] XCoordinates { get; }
        
        /// <summary>
        /// The y-coordinates of the points in this glyph. The first coordinates are relative to the origin (0, 0)
        /// the rest are relative to the previous point.
        /// </summary>
        public short[] YCoordinates { get; }

        public SimpleGlyphDescription(int instructionLength, int[] endPointsOfContours, SimpleGlyphFlags[] flags, short[] xCoordinates, short[] yCoordinates,
            PdfRectangle bounds)
        {
            InstructionLength = instructionLength;
            EndPointsOfContours = endPointsOfContours;
            Flags = flags;
            XCoordinates = xCoordinates;
            YCoordinates = yCoordinates;
            GlyphBounds = bounds;
        }

        public bool IsSimple { get; } = true;

        public SimpleGlyphDescription SimpleGlyph => this;

        public object CompositeGlyph { get; } = null;
    }
}