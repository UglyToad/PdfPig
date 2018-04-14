namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using Geometry;
    using Parser;
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

        public static GlyphDataTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, TableRegister tableRegister)
        {
            data.Seek(table.Offset);

            var indexToLocationTable = tableRegister.IndexToLocationTable;

            var offsets = indexToLocationTable.GlyphOffsets;

            var entryCount = offsets.Length;

            var glyphCount = entryCount - 1;

            var glyphs = new IGlyphDescription[glyphCount];

            var emptyGlyph = new EmptyGlyph(tableRegister.HeaderTable.Bounds);

            var compositeLocations = new Dictionary<int, TemporaryCompositeLocation>();

            for (var i = 0; i < glyphCount; i++)
            {
                if (offsets[i] == offsets[i + 1])
                {
                    // empty glyph
                    glyphs[i] = emptyGlyph;
                    continue;
                }

                data.Seek(offsets[i] + table.Offset);

                var contourCount = data.ReadSignedShort();

                var minX = data.ReadSignedShort();
                var minY = data.ReadSignedShort();
                var maxX = data.ReadSignedShort();
                var maxY = data.ReadSignedShort();
                
                var bounds = new PdfRectangle(minX, minY, maxX, maxY);
                
                // If the number of contours is greater than or equal zero it's a simple glyph.
                if (contourCount >= 0)
                {
                    glyphs[i] = ReadSimpleGlyph(data, contourCount, bounds);
                }
                else
                {
                    compositeLocations.Add(i , new TemporaryCompositeLocation(data.Position, bounds, contourCount));
                }
            }

            // Build composite glyphs by combining simple and other composite glyphs.
            foreach (var compositeLocation in compositeLocations)
            {
                glyphs[compositeLocation.Key] = ReadCompositeGlyph(data, compositeLocation.Value, compositeLocations, glyphs);
            }

            return new GlyphDataTable(table, glyphs);
        }

        private static SimpleGlyphDescription ReadSimpleGlyph(TrueTypeDataBytes data, short contourCount, PdfRectangle bounds)
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

            return new SimpleGlyphDescription(instructionLength, endPointsOfContours, flags, xCoordinates, yCoordinates, bounds);
        }

        private static CompositeGlyphDescription ReadCompositeGlyph(TrueTypeDataBytes data, TemporaryCompositeLocation compositeLocation, Dictionary<int, TemporaryCompositeLocation> compositeLocations, IGlyphDescription[] glyphs)
        {
            bool HasFlag(CompositeGlyphFlags value, CompositeGlyphFlags target)
            {
                return (value & target) == target;
            }

            data.Seek(compositeLocation.Position);

            CompositeGlyphFlags flags;
            do
            {
                flags = (CompositeGlyphFlags) data.ReadUnsignedShort();
                var glyphIndex = data.ReadUnsignedShort();

                var childGlyph = glyphs[glyphIndex];

                if (childGlyph == null)
                {
                    if (!compositeLocations.TryGetValue(glyphIndex, out var missingComposite))
                    {
                        throw new InvalidOperationException($"The composite glyph required a contour at index {glyphIndex} but there was no simple or composite glyph at this location.");
                    }

                    childGlyph = ReadCompositeGlyph(data, missingComposite, compositeLocations, glyphs);

                    glyphs[glyphIndex] = childGlyph;
                }

                var cloned = childGlyph.DeepClone();

                short arg1, arg2;
                if (HasFlag(flags, CompositeGlyphFlags.Args1And2AreWords))
                {
                    arg1 = data.ReadSignedShort();
                    arg2 = data.ReadSignedShort();
                }
                else
                {
                    arg1 = data.ReadByte();
                    arg2 = data.ReadByte();
                }

                float xscale = 1;
                float scale01 = 0;
                float scale10 = 0;
                float yscale = 1;

                bool hasScale, hasMatrix = false;
                if (HasFlag(flags, CompositeGlyphFlags.WeHaveAScale))
                {
                    xscale = ReadTwoFourteenFormat(data);
                    yscale = xscale;
                    hasScale = true;
                }
                else if (HasFlag(flags, CompositeGlyphFlags.WeHaveAnXAndYScale))
                {
                    xscale = ReadTwoFourteenFormat(data);
                    yscale = ReadTwoFourteenFormat(data);
                    hasScale = true;
                }
                else if (HasFlag(flags, CompositeGlyphFlags.WeHaveATwoByTwo))
                {
                    /*
                     * We build the 2 by 2 matrix:
                     * x 0
                     * 0 y
                     */
                    xscale = ReadTwoFourteenFormat(data);
                    scale01 = ReadTwoFourteenFormat(data);
                    scale10 = ReadTwoFourteenFormat(data);
                    yscale = ReadTwoFourteenFormat(data);
                    hasScale = true;
                    hasMatrix = true;
                }

                if (HasFlag(flags, CompositeGlyphFlags.ArgsAreXAndYValues))
                {
                    
                }
                else
                {
                    // TODO: Not implemented, it is unclear how to do this.
                }

            } while (HasFlag(flags, CompositeGlyphFlags.MoreComponents));

            return new CompositeGlyphDescription();
        }

        private static float ReadTwoFourteenFormat(TrueTypeDataBytes data)
        {
            const float divisor = 1 << 14;

            return data.ReadSignedShort() / divisor;
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

                xs[i] = (short)x;
            }

            return xs;
        }

        /// <summary>
        /// Stores the composite glyph information we read when initially scanning the glyph table.
        /// Once we have all composite glyphs we can start building them from simple glyphs.
        /// </summary>
        private struct TemporaryCompositeLocation
        {
            /// <summary>
            /// Stores the position after reading the contour count and bounds.
            /// </summary>
            public long Position { get; }

            /// <summary>
            /// The bounds we read.
            /// </summary>
            public PdfRectangle Bounds { get; }

            /// <summary>
            /// The number of contours in this composite glyph. Should be less than zero.
            /// </summary>
            public short ContourCount { get; }
            
            public TemporaryCompositeLocation(long position, PdfRectangle bounds, short contourCount)
            {
                Position = position;
                Bounds = bounds;
                ContourCount = contourCount;

                if (ContourCount >= 0 )
                {
                    throw new ArgumentException($"A composite glyph should not have a positive contour count. Got: {contourCount}.", nameof(contourCount));
                }
            }
        }
    }
}
