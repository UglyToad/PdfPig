namespace UglyToad.Pdf.Fonts.TrueType.Tables
{
    using System;

    /// <summary>
    /// Describes the glyphs in the font.
    /// </summary>
    internal class GlyphDataTable : ITable
    {
        public string Tag => TrueTypeHeaderTable.Glyf;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public static GlyphDataTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, HeaderTable headerTable,
            IndexToLocationTable indexToLocationTable)
        {
            data.Seek(table.Offset - 1);

            var offsets = indexToLocationTable.GlyphOffsets;

            var entryCount = offsets.Length;

            var glyphCount = entryCount - 1;

            var glyphs = new object[glyphCount];

            for (var i = 0; i < glyphCount; i++)
            {
                if (offsets[i] == offsets[i + 1])
                {
                    // empty glyph
                    continue;
                }

                data.Seek(offsets[i] - 1);

                var contourCount = data.ReadSignedShort();

                var minX = data.ReadSignedShort();
                var minY = data.ReadSignedShort();
                var maxX = data.ReadSignedShort();
                var maxY = data.ReadSignedShort();

                var bounds = new TrueTypeGlyphBounds(minX, minY, maxX, maxY);

                // If the number of contours is greater than or equal zero it's a simple glyph.
                if (contourCount >= 0)
                {
                    glyphs[i] = ReadSimpleGlyph(data, contourCount, bounds);
                }
                else
                {
                    
                }
            }

            throw new NotImplementedException();
        }

        private static SimpleGlyphDescription ReadSimpleGlyph(TrueTypeDataBytes data, short contourCount, TrueTypeGlyphBounds bounds)
        {
            throw new NotImplementedException("Reading simple glyphs not supported yet.");
        }
    }

    internal class SimpleGlyphDescription
    {
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

        public SimpleGlyphDescription(int instructionLength, int[] endPointsOfContours, SimpleGlyphFlags[] flags, short[] xCoordinates, short[] yCoordinates)
        {
            InstructionLength = instructionLength;
            EndPointsOfContours = endPointsOfContours;
            Flags = flags;
            XCoordinates = xCoordinates;
            YCoordinates = yCoordinates;
        }
    }

    [Flags]
    internal enum SimpleGlyphFlags : byte
    {
        /// <summary>
        /// The point is on the curve.
        /// </summary>
        OnCurve = 1,
        /// <summary>
        /// The x-coordinate is 1 byte long instead of 2.
        /// </summary>
        XShortVector = 1 << 1,
        /// <summary>
        /// The y-coordinate is 1 byte long instead of 2.
        /// </summary>
        YShortVector = 1 << 2,
        /// <summary>
        /// The next byte specifies the number of times to repeat this set of flags.
        /// </summary>
        Repeat = 1 << 3,
        /// <summary>
        /// If <see cref="XShortVector"/> is set this means the sign of the x-coordinate is positive.
        /// If <see cref="XShortVector"/> is not set then the current x-coordinate is the same as the previous.
        /// </summary>
        XSignOrSame = 1 << 4,
        /// <summary>
        /// If <see cref="YShortVector"/> is set this means the sign of the y-coordinate is positive.
        /// If <see cref="YShortVector"/> is not set then the current y-coordinate is the same as the previous.
        /// </summary>
        YSignOrSame = 1 << 5
    }

    internal struct TrueTypeGlyphBounds
    {
        public short X1 { get; }

        public short Y1 { get; }

        public short X2 { get; }

        public short Y2 { get; }

        public TrueTypeGlyphBounds(short x1, short y1, short x2, short y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
    }
}
