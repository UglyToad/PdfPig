namespace UglyToad.PdfPig.Tests.Fonts.TrueType
{
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Fonts.TrueType.Parser;
    using UglyToad.PdfPig.Fonts.TrueType;

    /// <summary>
    /// Composite nesting and cycles, and the limits on points kept per glyph and per glyph table.
    /// </summary>
    public class GlyphDataTableLimitsTests
    {
        [Fact]
        public void DeepCompositeChainResolves()
        {
            // Glyphs 1 to 5000 are composites of glyph i + 1, glyph 5001 is a simple square.
            const int chainLength = 5000;
            var composites = new int[chainLength + 1][];
            for (var i = 1; i <= chainLength; i++)
            {
                composites[i] = [i + 1];
            }

            var font = Parse(composites);

            // Both ends of the chain resolve to the square.
            Assert.True(font.TryGetPath(1, c => c, out var deepest));
            Assert.Single(deepest);

            Assert.True(font.TryGetPath(chainLength, c => c, out var shallowest));
            Assert.Single(shallowest);
        }

        [Fact]
        public void CompositeCycleBelowFirstChildResolves()
        {
            // 1 -> 2 -> 3 -> 4 -> 3: the cycle does not include the first child of glyph 1.
            var font = Parse([null, [2], [3], [4], [3]]);

            for (var i = 1; i <= 4; i++)
            {
                Assert.True(font.TryGetPath(i, c => c, out _));
            }
        }

        [Fact]
        public void CompositeCycleKeepsOtherComponents()
        {
            // Glyph 1 is glyph 2 plus the square (glyph 3), and glyph 2 refers back to glyph 1.
            var font = Parse([null, [2, 3], [1]]);

            // The cycle reads as empty, so glyph 1 is the square alone.
            Assert.True(font.TryGetPath(1, c => c, out var path));
            Assert.Single(path);

            Assert.True(font.TryGetPath(2, c => c, out var cyclic));
            Assert.Empty(cyclic);
        }

        [Fact]
        public void CompositePointCountDoublingPerLevelIsBounded()
        {
            // Glyph i uses glyph i + 1 twice, so glyph i has 4 * 2^(25 - i) points.
            const int levels = 24;
            var composites = new int[levels + 1][];
            for (var i = 1; i <= levels; i++)
            {
                composites[i] = [i + 1, i + 1];
            }

            var font = Parse(composites);

            // Past 65,536 points the glyph is dropped.
            Assert.True(font.TryGetPath(1, c => c, out var tooLarge));
            Assert.Empty(tooLarge);

            // 4 * 2^14 = 65,536 points is the largest glyph kept.
            Assert.True(font.TryGetPath(levels - 13, c => c, out var largest));
            Assert.Equal(1 << 14, largest.Count);

            Assert.True(font.TryGetPath(levels - 14, c => c, out var justTooLarge));
            Assert.Empty(justTooLarge);
        }

        [Fact]
        public void CompositeWithSeveralComponentsKeepsEachContour()
        {
            // Glyph 1 is the square drawn three times, offset by 0, 200 and 400 on x.
            var font = Parse([null, [2, 2, 2]], [0, 0, 200, 0, 400, 0]);

            Assert.True(font.TryGetPath(1, c => c, out var path));
            Assert.Equal(3, path.Count);

            var starts = path.Select(p => p.Commands.OfType<PdfSubpath.Move>().First().Location).ToArray();
            Assert.Equal(new PdfPoint(0, 0), starts[0]);
            Assert.Equal(new PdfPoint(200, 0), starts[1]);
            Assert.Equal(new PdfPoint(400, 0), starts[2]);
        }

        [Fact]
        public void ManyCompositesOfALargeGlyphStayWithinTheTableBudget()
        {
            // Glyph 1 is the square repeated 16,384 times (65,536 points) and glyphs 2 to 501 each use glyph 1 once.
            const int copies = 500;
            const int squareIndex = copies + 2;
            var glyphs = new List<byte[]> { MakeComposite(Enumerable.Repeat(squareIndex, 16384).ToArray(), null) };
            for (var i = 0; i < copies; i++)
            {
                glyphs.Add(MakeComposite([1], null));
            }

            glyphs.Add(CompositeInt8OffsetsTests.MakeSimpleSquare(0, 0, 100, 100));

            var font = ParseGlyphs(glyphs);

            Assert.True(font.TryGetPath(1, c => c, out var first));
            Assert.Equal(16384, first.Count);

            // Once the budget is spent the remaining glyphs are kept empty.
            Assert.InRange(CountDrawnGlyphs(font, 1, copies + 1), 2, 16);
        }

        [Fact]
        public void ManyLargeSimpleGlyphsStayWithinTheTableBudget()
        {
            // 200 glyphs of 65,536 points each.
            const int count = 200;
            var font = ParseGlyphs(Enumerable.Range(0, count).Select(_ => MakeLargeSimpleGlyph()).ToList());

            Assert.InRange(CountDrawnGlyphs(font, 1, count), 1, 16);
        }

        private static int CountDrawnGlyphs(TrueTypeFont font, int first, int count)
        {
            return Enumerable.Range(first, count).Count(g => font.TryGetPath(g, c => c, out var path) && path.Count > 0);
        }

        /// <summary>
        /// Glyph 0 is empty, glyph i (for i >= 1) is a composite of the glyphs in composites[i],
        /// and a simple square glyph follows them at index composites.Length.
        /// </summary>
        private static TrueTypeFont Parse(int[]?[] composites, int[]? offsets = null)
        {
            var glyphs = new List<byte[]>();
            for (var i = 1; i < composites.Length; i++)
            {
                glyphs.Add(MakeComposite(composites[i]!, offsets));
            }

            glyphs.Add(CompositeInt8OffsetsTests.MakeSimpleSquare(0, 0, 100, 100));

            return ParseGlyphs(glyphs);
        }

        /// <summary>
        /// Glyph 0 is empty and glyph i (for i >= 1) is glyphs[i - 1].
        /// </summary>
        private static TrueTypeFont ParseGlyphs(IReadOnlyList<byte[]> glyphs)
        {
            var glyf = new List<byte>();
            var loca = new List<uint> { 0 };

            foreach (var glyph in glyphs)
            {
                loca.Add((uint)glyf.Count);
                glyf.AddRange(glyph);
                while (glyf.Count % 4 != 0)
                {
                    glyf.Add(0);
                }
            }

            loca.Add((uint)glyf.Count);

            var bytes = CompositeInt8OffsetsTests.AssembleSfnt(glyphs.Count + 1, glyf.ToArray(), loca.ToArray());

            return TrueTypeFontParser.Parse(new TrueTypeDataBytes(bytes));
        }

        private static byte[] MakeLargeSimpleGlyph()
        {
            var result = new List<byte>
            {
                0, 1, // numberOfContours = 1
                0, 0, 0, 0, 0, 0, 0, 0, // bbox
                0xFF, 0xFF, // endPoint[0] = 65535
                0, 0, // instructionLength = 0
            };

            // 256 runs of 256 points: on curve, x and y unchanged (no coordinate bytes), repeated 255 more times.
            for (var i = 0; i < 256; i++)
            {
                result.Add(0x39);
                result.Add(255);
            }

            return result.ToArray();
        }

        private static byte[] MakeComposite(int[] glyphIndices, int[]? offsets)
        {
            var result = new List<byte>
            {
                0xFF, 0xFF, // numberOfContours = -1
                0, 0, 0, 0, 0, 0, 0, 0, // bbox
            };

            for (var i = 0; i < glyphIndices.Length; i++)
            {
                var more = i < glyphIndices.Length - 1;
                var dx = (short)(offsets?[2 * i] ?? 0);
                var dy = (short)(offsets?[2 * i + 1] ?? 0);

                // flags = ArgsAreXAndYValues | Args1And2AreWords, plus MoreComponents when another follows
                result.AddRange([0x00, (byte)(more ? 0x23 : 0x03)]);
                result.AddRange([(byte)(glyphIndices[i] >> 8), (byte)glyphIndices[i]]);
                result.AddRange([(byte)(dx >> 8), (byte)dx, (byte)(dy >> 8), (byte)dy]);
            }

            while (result.Count % 4 != 0)
            {
                result.Add(0);
            }

            return result.ToArray();
        }
    }
}
