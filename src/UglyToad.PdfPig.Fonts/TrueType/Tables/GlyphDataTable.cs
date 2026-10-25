namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Glyphs;
    using Parser;

    /// <inheritdoc />
    /// <summary>
    /// The 'glyf' table contains the data that defines the appearance of the glyphs in the font. 
    /// This includes specification of the points that describe the contours that make up a glyph outline and the instructions that grid-fit that glyph.
    /// </summary>
    internal sealed class GlyphDataTable : ITrueTypeTable
    {
        private const int MaxGlyphPoints = ushort.MaxValue + 1;

        private const long MinTablePointBudget = 1 << 20;
        private const long TablePointBudgetPerByte = 4;

        private readonly IReadOnlyList<uint> glyphOffsets;
        private readonly PdfRectangle maxGlyphBounds;
        private TrueTypeDataBytes tableBytes;

        /// <inheritdoc />
        public string Tag => TrueTypeHeaderTable.Glyf;

        /// <inheritdoc />
        public TrueTypeHeaderTable DirectoryTable { get; }

        private readonly Lazy<IReadOnlyList<IGlyphDescription>> glyphs;
        public IReadOnlyList<IGlyphDescription> Glyphs => glyphs.Value;

        public GlyphDataTable(TrueTypeHeaderTable directoryTable, IReadOnlyList<uint> glyphOffsets,
            PdfRectangle maxGlyphBounds,
            TrueTypeDataBytes tableBytes)
        {
            this.glyphOffsets = glyphOffsets;
            this.maxGlyphBounds = maxGlyphBounds;
            this.tableBytes = tableBytes;
            DirectoryTable = directoryTable;
            if (tableBytes == null)
            {
                throw new ArgumentNullException(nameof(tableBytes));
            }

            if (glyphOffsets == null)
            {
                throw new ArgumentNullException(nameof(glyphOffsets));
            }

            if (tableBytes.Length != directoryTable.Length)
            {
                throw new ArgumentException($"glyf table data should match length of directory entry. Expected: {directoryTable.Length}. Actual: {tableBytes.Length}.");
            }

            glyphs = new Lazy<IReadOnlyList<IGlyphDescription>>(ReadGlyphs);
        }

        public bool TryGetGlyphBounds(int glyphIndex, out PdfRectangle bounds)
        {
            bounds = default(PdfRectangle);

            if (glyphIndex < 0 || glyphIndex >= glyphOffsets.Count - 1)
            {
                return false;
            }

            var offset = glyphOffsets[glyphIndex];
            var nextOffset = glyphOffsets[glyphIndex + 1];

            if (nextOffset <= offset)
            {
                bounds = new PdfRectangle(0, 0, 0, 0);
                return true;
            }

            bounds = Glyphs[glyphIndex].Bounds;

            return true;
        }

        public bool TryGetGlyphPath(int glyphIndex, out IReadOnlyList<PdfSubpath> subpaths)
        {
            subpaths = null;

            if (glyphIndex < 0 || glyphIndex > Glyphs.Count - 1)
            {
                return false;
            }

            return Glyphs[glyphIndex].TryGetGlyphPath(out subpaths);
        }

        public static GlyphDataTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, TableRegister.Builder tableRegister)
        {
            data.Seek(table.Offset);

            var bytes = data.ReadByteArray((int)table.Length);

            return new GlyphDataTable(table, tableRegister.IndexToLocationTable.GlyphOffsets,
                tableRegister.HeaderTable.Bounds,
                new TrueTypeDataBytes(bytes));
        }

        private IReadOnlyList<IGlyphDescription> ReadGlyphs()
        {
            if (tableBytes == null)
            {
                throw new InvalidOperationException("Bytes cache was discarded before lazy value evaluated.");
            }

            var data = tableBytes;

            var offsets = glyphOffsets;

            var entryCount = offsets.Count;

            var glyphCount = entryCount - 1;

            var result = new IGlyphDescription[glyphCount];

            var emptyGlyph = Glyph.Empty(maxGlyphBounds);

            // Glyphs that do not fit in what is left of the budget are kept empty.
            var pointBudget = Math.Max(MinTablePointBudget, TablePointBudgetPerByte * data.Length);

            var composites = new Dictionary<int, TemporaryComposite>();

            for (var i = 0; i < glyphCount; i++)
            {
                var offset = offsets[i];

                if (offsets[i + 1] <= offset)
                {
                    // empty glyph
                    result[i] = emptyGlyph;
                    continue;
                }

                // Invalid table, just sub in the empty glyph
                if (offset >= data.Length)
                {
                    result[i] = emptyGlyph;
                    continue;
                }

                data.Seek(offset);

                var contourCount = data.ReadSignedShort();

                var minX = data.ReadSignedShort();
                var minY = data.ReadSignedShort();
                var maxX = data.ReadSignedShort();
                var maxY = data.ReadSignedShort();

                var bounds = new PdfRectangle(minX, minY, maxX, maxY);

                // If the number of contours is greater than or equal zero it's a simple glyph.
                if (contourCount >= 0)
                {
                    result[i] = ReadSimpleGlyph(data, contourCount, bounds, pointBudget, emptyGlyph);
                    pointBudget -= PointCost(result[i]);
                }
                else
                {
                    composites.Add(i, new TemporaryComposite(bounds, contourCount, ReadCompositeComponents(data, glyphCount)));
                }
            }

            // Build composite glyphs by combining simple and other composite glyphs.
            var pending = new Stack<(int Index, int NextComponent)>();
            foreach (var root in composites)
            {
                if (result[root.Key] is not null)
                {
                    // Already built as a component of an earlier composite.
                    continue;
                }

                // A composite holds the empty glyph while it is on the stack, so a cycle back to it reads as empty.
                result[root.Key] = emptyGlyph;
                pending.Push((root.Key, 0));

                while (pending.Count > 0)
                {
                    var (index, next) = pending.Pop();
                    var components = composites[index].Components;

                    while (next < components.Count && result[components[next].Index] is not null)
                    {
                        next++;
                    }

                    if (next < components.Count)
                    {
                        // Only composites are still unbuilt: build this component first, then come back.
                        var child = components[next].Index;
                        pending.Push((index, next + 1));
                        result[child] = emptyGlyph;
                        pending.Push((child, 0));
                        continue;
                    }

                    result[index] = BuildCompositeGlyph(composites[index], result, emptyGlyph, pointBudget);
                    pointBudget -= PointCost(result[index]);
                }
            }

            tableBytes = null;

            return result;

            static long PointCost(IGlyphDescription glyph)
            {
                return glyph.Points.Length + glyph.EndPointsOfContours.Length;
            }
        }

        private static IGlyphDescription ReadSimpleGlyph(TrueTypeDataBytes data, short contourCount, PdfRectangle bounds, long pointBudget, IGlyphDescription emptyGlyph)
        {
            if (contourCount == 0)
            {
                return new Glyph(true, [], [], [], new PdfRectangle(0, 0, 0, 0));
            }

            var endPointsOfContours = data.ReadUnsignedShortArray(contourCount);

            var instructionLength = data.ReadUnsignedShort();

            var instructions = data.ReadByteArray(instructionLength);

            var pointCount = 0;
            if (contourCount > 0)
            {
                pointCount = endPointsOfContours[contourCount - 1] + 1;
            }

            if (pointCount + contourCount > pointBudget)
            {
                return emptyGlyph;
            }

            var flags = ReadFlags(data, pointCount);

            var xCoordinates = ReadCoordinates(data, pointCount, flags, SimpleGlyphFlags.XSingleByte,
                SimpleGlyphFlags.ThisXIsTheSame);

            var yCoordinates = ReadCoordinates(data, pointCount, flags, SimpleGlyphFlags.YSingleByte,
                SimpleGlyphFlags.ThisYIsTheSame);

            int endPtIndex = endPointsOfContours.Length - 1;
            int endPtOfContourIndex = -1;
            var points = new GlyphPoint[xCoordinates.Length];

            for (var i = xCoordinates.Length - 1; i >= 0; i--)
            {
                if (endPtOfContourIndex == -1)
                {
                    endPtOfContourIndex = endPointsOfContours[endPtIndex];
                }
                bool endPt = endPtOfContourIndex == i;
                if (endPt && endPtIndex > 0)
                {
                    endPtIndex--;
                    endPtOfContourIndex = -1;
                }

                var isOnCurve = (flags[i] & SimpleGlyphFlags.OnCurve) == SimpleGlyphFlags.OnCurve;
                points[i] = new GlyphPoint(xCoordinates[i], yCoordinates[i], isOnCurve, endPt);
            }

            return new Glyph(true, instructions, endPointsOfContours, points, bounds);
        }

        private static List<CompositeComponent> ReadCompositeComponents(TrueTypeDataBytes data, int glyphCount)
        {
            bool HasFlag(CompositeGlyphFlags value, CompositeGlyphFlags target)
            {
                return (value & target) == target;
            }

            var components = new List<CompositeComponent>();

            CompositeGlyphFlags flags;
            do
            {
                flags = (CompositeGlyphFlags)data.ReadUnsignedShort();
                var glyphIndex = data.ReadUnsignedShort();

                if (glyphIndex >= glyphCount)
                {
                    // Unsure why this happens but fixes #1213
                    continue; // TODO - Is there a better fix?
                }

                short arg1, arg2;
                if (HasFlag(flags, CompositeGlyphFlags.Args1And2AreWords))
                {
                    arg1 = data.ReadSignedShort();
                    arg2 = data.ReadSignedShort();
                }
                else
                {
                    if (HasFlag(flags, CompositeGlyphFlags.ArgsAreXAndYValues))
                    {
                        arg1 = (sbyte)data.ReadByte();
                        arg2 = (sbyte)data.ReadByte();
                    }
                    else
                    {
                        arg1 = data.ReadByte();
                        arg2 = data.ReadByte();
                    }
                }

                double xscale = 1;
                double scale01 = 0;
                double scale10 = 0;
                double yscale = 1;

                if (HasFlag(flags, CompositeGlyphFlags.WeHaveAScale))
                {
                    xscale = ReadTwoFourteenFormat(data);
                    yscale = xscale;
                }
                else if (HasFlag(flags, CompositeGlyphFlags.WeHaveAnXAndYScale))
                {
                    xscale = ReadTwoFourteenFormat(data);
                    yscale = ReadTwoFourteenFormat(data);
                }
                else if (HasFlag(flags, CompositeGlyphFlags.WeHaveATwoByTwo))
                {
                    xscale = ReadTwoFourteenFormat(data);
                    scale01 = ReadTwoFourteenFormat(data);
                    scale10 = ReadTwoFourteenFormat(data);
                    yscale = ReadTwoFourteenFormat(data);
                }

                if (HasFlag(flags, CompositeGlyphFlags.ArgsAreXAndYValues))
                {
                    components.Add(new CompositeComponent(glyphIndex, new CompositeTransformMatrix3By2(xscale, scale01, scale10, yscale, arg1, arg2)));
                }
                else
                {
                    // TODO: Not implemented, it is unclear how to do this.
                }
            } while (HasFlag(flags, CompositeGlyphFlags.MoreComponents));

            return components;
        }

        private static IGlyphDescription BuildCompositeGlyph(TemporaryComposite composite, IGlyphDescription[] glyphs, IGlyphDescription emptyGlyph, long pointBudget)
        {
            var components = composite.Components;

            if (components.Count == 0)
            {
                return new Glyph(false, emptyGlyph.Instructions, emptyGlyph.EndPointsOfContours, emptyGlyph.Points, composite.Bounds);
            }

            var pointCount = 0;
            var contourCount = 0;
            foreach (var component in components)
            {
                var glyph = glyphs[component.Index];
                pointCount += glyph.Points.Length;
                contourCount += glyph.EndPointsOfContours.Length;

                if (pointCount > MaxGlyphPoints || contourCount > MaxGlyphPoints || pointCount + contourCount > pointBudget)
                {
                    return emptyGlyph;
                }
            }

            // Fill the arrays once, writing each component's transformed points straight into them.
            var points = new GlyphPoint[pointCount];
            var endPointsOfContours = new ushort[contourCount];
            var pointOffset = 0;
            var contourOffset = 0;
            foreach (var component in components)
            {
                var glyph = glyphs[component.Index];
                var matrix = component.Transformation;

                foreach (var endPoint in glyph.EndPointsOfContours)
                {
                    endPointsOfContours[contourOffset++] = (ushort)(endPoint + pointOffset);
                }

                foreach (var point in glyph.Points)
                {
                    var transformed = matrix.Translate(matrix.ScaleAndRotate(new PdfPoint(point.X, point.Y)));
                    points[pointOffset++] = new GlyphPoint((short)transformed.X, (short)transformed.Y, point.IsOnCurve, point.IsEndOfContour);
                }
            }

            return new Glyph(false, glyphs[components[0].Index].Instructions, endPointsOfContours, points, composite.Bounds);
        }

        private static SimpleGlyphFlags[] ReadFlags(TrueTypeDataBytes data, int pointCount)
        {
            var result = new SimpleGlyphFlags[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                result[i] = (SimpleGlyphFlags)data.ReadByte();

                if (result[i].HasFlag(SimpleGlyphFlags.Repeat))
                {
                    var numberOfRepeats = data.ReadByte();

                    for (int j = 0; j < numberOfRepeats; j++)
                    {
                        int p = i + j + 1;
                        if (p >= result.Length)
                        {
                            // Unsure why this happens but fixes #1199
                            // TODO - Is there a better fix?
                            break;
                        }

                        result[p] = result[i];
                    }

                    i += numberOfRepeats;
                }
            }

            return result;
        }

        private static short[] ReadCoordinates(TrueTypeDataBytes data, int pointCount, SimpleGlyphFlags[] flags, SimpleGlyphFlags isByte, SimpleGlyphFlags signOrSame)
        {
            bool HasFlag(SimpleGlyphFlags value, SimpleGlyphFlags target)
            {
                return (value & target) == target;
            }

            var xs = new short[pointCount];
            var x = 0;
            for (var i = 0; i < pointCount; i++)
            {
                var flag = flags[i];
                int dx;
                if (HasFlag(flag, isByte))
                {
                    var b = data.ReadByte();
                    dx = HasFlag(flag, signOrSame) ? b : -b;
                }
                else
                {
                    if (HasFlag(flag, signOrSame))
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

        private static double ReadTwoFourteenFormat(TrueTypeDataBytes data)
        {
            const double divisor = 1 << 14;

            return data.ReadSignedShort() / divisor;
        }

        /// <summary>
        /// Stores the composite glyph information we read when initially scanning the glyph table.
        /// Once we have all composite glyphs we can start building them from simple glyphs.
        /// </summary>
        private readonly struct TemporaryComposite
        {
            public PdfRectangle Bounds { get; }

            public List<CompositeComponent> Components { get; }

            public TemporaryComposite(PdfRectangle bounds, short contourCount, List<CompositeComponent> components)
            {
                if (contourCount >= 0)
                {
                    throw new ArgumentException($"A composite glyph should not have a positive contour count. Got: {contourCount}.", nameof(contourCount));
                }

                Bounds = bounds;
                Components = components;
            }
        }

        private sealed class CompositeComponent
        {
            public int Index { get; }

            public CompositeTransformMatrix3By2 Transformation { get; }

            public CompositeComponent(int index, CompositeTransformMatrix3By2 transformation)
            {
                Index = index;
                Transformation = transformation;
            }
        }
    }
}
