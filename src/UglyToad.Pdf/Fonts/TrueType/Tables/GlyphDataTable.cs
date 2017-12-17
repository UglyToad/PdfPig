namespace UglyToad.Pdf.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Describes the glyphs in the font.
    /// </summary>
    internal class GlyphDataTable : ITable
    {
        public string Tag => TrueTypeHeaderTable.Glyf;

        public TrueTypeHeaderTable DirectoryTable { get; }

        [ItemCanBeNull]
        public IReadOnlyList<IGlyphDescription> Glyphs { get; }

        public GlyphDataTable(TrueTypeHeaderTable directoryTable, IReadOnlyList<IGlyphDescription> glyphs)
        {
            DirectoryTable = directoryTable;
            Glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));
        }

        public static GlyphDataTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, HeaderTable headerTable,
            IndexToLocationTable indexToLocationTable)
        {
            data.Seek(table.Offset - 1);

            var offsets = indexToLocationTable.GlyphOffsets;

            var entryCount = offsets.Length;

            var glyphCount = entryCount - 1;

            var glyphs = new IGlyphDescription[glyphCount];

            for (var i = 0; i < glyphCount; i++)
            {
                if (offsets[i] == offsets[i + 1])
                {
                    // empty glyph
                    continue;
                }

                data.Seek(offsets[i] - 1 + table.Offset);

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

            return new GlyphDataTable(table, glyphs);
        }

        private static SimpleGlyphDescription ReadSimpleGlyph(TrueTypeDataBytes data, short contourCount, TrueTypeGlyphBounds bounds)
        {
            var endPointsOfContours = data.ReadUnsignedShortArray(contourCount);

            var instructionLength = data.ReadUnsignedShort();

            data.ReadByteArray(instructionLength);

            var pointCount = 0;
            if (contourCount > 0)
            {
                pointCount = endPointsOfContours[contourCount - 1] + 1;
            }

            var flags = ReadFlags(data, pointCount);

            var xCoordinates = ReadCoordinates(data, pointCount, flags, SimpleGlyphFlags.XShortVector,
                SimpleGlyphFlags.XSignOrSame);

            var yCoordinates = ReadCoordinates(data, pointCount, flags, SimpleGlyphFlags.YShortVector,
                SimpleGlyphFlags.YSignOrSame);

            return new SimpleGlyphDescription(instructionLength, endPointsOfContours, flags, xCoordinates, yCoordinates);
        }

        private static SimpleGlyphFlags[] ReadFlags(TrueTypeDataBytes data, int pointCount)
        {
            var result = new SimpleGlyphFlags[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                result[i] = (SimpleGlyphFlags)data.ReadByte();
            }

            return result;
        }

        private static short[] ReadCoordinates(TrueTypeDataBytes data, int pointCount, SimpleGlyphFlags[] flags, SimpleGlyphFlags isByte, SimpleGlyphFlags signOrSame)
        {
            var xs = new short[pointCount];
            var x = 0;
            for (var i = 0; i < pointCount; i++)
            {
                int dx;
                if (flags[i].HasFlag(isByte))
                {
                    var b = data.ReadByte();
                    dx = flags[i].HasFlag(signOrSame) ? b : -b;
                }
                else
                {
                    if (flags[i].HasFlag(signOrSame))
                    {
                        dx = 0;
                    }
                    else
                    {
                        dx = data.ReadSignedShort();
                    }
                }

                x += dx;

                // TODO: overflow?
                xs[i] = (short)x;
            }

            return xs;
        }
    }

    internal interface IGlyphDescription
    {
        bool IsSimple { get; }

        SimpleGlyphDescription SimpleGlyph { get; }

        object CompositeGlyph { get; }
    }

    internal class SimpleGlyphDescription : IGlyphDescription
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

        public bool IsSimple { get; } = true;

        public SimpleGlyphDescription SimpleGlyph => this;

        public object CompositeGlyph { get; } = null;
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
