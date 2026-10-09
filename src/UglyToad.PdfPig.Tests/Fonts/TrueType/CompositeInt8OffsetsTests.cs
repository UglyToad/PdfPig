namespace UglyToad.PdfPig.Tests.Fonts.TrueType
{
    using System.Buffers.Binary;
    using UglyToad.PdfPig.Fonts.TrueType.Parser;
    using UglyToad.PdfPig.Fonts.TrueType;

    public class CompositeInt8OffsetsTests
    {
        [Fact]
        public void CompositeWithSignedByteOffsets_ReadsCorrectly()
        {
            var bytes = BuildFontWithSignedByteOffsets();
            var font = TrueTypeFontParser.Parse(new TrueTypeDataBytes(bytes));

            // First verify glyph 1 (the simple square) works
            bool result1 = font.TryGetPath(1, c => c, out var path1);
            Assert.True(result1);
            Assert.NotNull(path1);
            Assert.Single(path1);

            // Glyph 2 is a composite referencing glyph 1 with signed byte offsets (-10, -20)
            bool result2 = font.TryGetPath(2, c => c, out var path2);
            Assert.True(result2);
            Assert.NotNull(path2);
            Assert.Single(path2);

            var subpath = path2[0];
            var commands = subpath.Commands.ToList();
            var firstMove = commands.OfType<UglyToad.PdfPig.Core.PdfSubpath.Move>().First();
            // With signed offsets (-10, -20), the starting point should be (-10, -20)
            // With unsigned offsets (246, 236), it would be (246, 236)
            Assert.Equal(-10, firstMove.Location.X, 0);
            Assert.Equal(-20, firstMove.Location.Y, 0);
        }

        private static byte[] BuildFontWithSignedByteOffsets()
        {
            // Glyph 0: empty
            // Glyph 1: simple square at (0,0), (100,0), (100,100), (0,100)
            // Glyph 2: composite referencing glyph 1 with signed byte offsets (-10, -20)

            var glyf = new List<byte>();
            var loca = new List<uint>();

            // Glyph 0: empty
            loca.Add((uint)glyf.Count);

            // Glyph 1: simple square
            loca.Add((uint)glyf.Count);
            glyf.AddRange(MakeSimpleSquare(0, 0, 100, 100));
            while (glyf.Count % 4 != 0) glyf.Add(0);

            // Glyph 2: composite with signed byte offsets
            loca.Add((uint)glyf.Count);
            glyf.AddRange(MakeCompositeWithSignedByteOffsets(1, -10, -20));
            while (glyf.Count % 4 != 0) glyf.Add(0);

            loca.Add((uint)glyf.Count);

            return AssembleSfnt(3, glyf.ToArray(), loca.ToArray());
        }

        private static byte[] MakeSimpleSquare(int xMin, int yMin, int xMax, int yMax)
        {
            return
            [
                0, 1, // numberOfContours = 1
                (byte)(xMin >> 8), (byte)xMin, // xMin
                (byte)(yMin >> 8), (byte)yMin, // yMin
                (byte)(xMax >> 8), (byte)xMax, // xMax
                (byte)(yMax >> 8), (byte)yMax, // yMax
                0, 3, // endPoint[0] = 3
                0, 0, // instructionLength = 0
                0x01, 0x01, 0x01, 0x01, // flags: all on-curve
                // xCoordinates (delta encoded as shorts)
                0, 0, // delta 0 (first point at xMin)
                (byte)((xMax - xMin) >> 8), (byte)(xMax - xMin), // delta to xMax
                0, 0, // delta 0
                (byte)(((xMin - xMax) >> 8) & 0xFF), (byte)((xMin - xMax) & 0xFF), // delta back to xMin
                // yCoordinates (delta encoded as shorts)
                0, 0, // delta 0 (first point at yMin)
                0, 0, // delta 0
                (byte)((yMax - yMin) >> 8), (byte)(yMax - yMin), // delta to yMax
                0, 0, // delta 0
            ];
        }

        private static byte[] MakeCompositeWithSignedByteOffsets(ushort glyphIndex, short offsetX, short offsetY)
        {
            // Composite glyph with ARGS_ARE_XY_VALUES set, ARG_1_AND_2_ARE_WORDS clear
            // Arguments are signed bytes: offsetX, offsetY
            byte arg1 = (byte)(sbyte)offsetX; // -10 -> 0xF6
            byte arg2 = (byte)(sbyte)offsetY; // -20 -> 0xEC

            return
            [
                0xFF, 0xFF, // numberOfContours = -1
                0, 0, 0, 0, 0, 0, 0, 0, // bbox
                0x00, 0x02, // flags = ArgsAreXAndYValues (NOT Args1And2AreWords)
                (byte)(glyphIndex >> 8), (byte)glyphIndex, // glyphIndex
                arg1, arg2, // arguments as signed bytes
            ];
        }

        private static byte[] BE16(ushort v) => [(byte)(v >> 8), (byte)v];
        private static byte[] BE32(uint v) => [(byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v];

        private static byte[] AssembleSfnt(int numGlyphs, byte[] glyfData, uint[] locaValues)
        {
            var tables = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
            tables["glyf"] = glyfData;
            tables["head"] = Head(1000);
            tables["hhea"] = Hhea(numGlyphs);
            tables["hmtx"] = Enumerable.Range(0, numGlyphs)
                .SelectMany(_ => BE16(1000).Concat(BE16(0))).ToArray();
            tables["loca"] = locaValues.SelectMany(BE32).ToArray();
            tables["maxp"] = Maxp(numGlyphs);
            tables["name"] = [0, 0, 0, 0, 0, 6];
            tables["post"] = Post();

            int numTables = tables.Count;
            int pos = 12 + 16 * numTables;
            var dir = new List<byte>();
            var body = new List<byte>();
            foreach (var (tag, data) in tables)
            {
                dir.AddRange(System.Text.Encoding.Latin1.GetBytes(tag));
                dir.AddRange(BE32(Checksum(data)));
                dir.AddRange(BE32((uint)(pos + body.Count)));
                dir.AddRange(BE32((uint)data.Length));
                body.AddRange(data);
                while (body.Count % 4 != 0) body.Add(0);
            }

            int es = 0;
            while ((2 << es) <= numTables) es++;
            var file = new List<byte>();
            file.AddRange(BE32(0x00010000));
            file.AddRange(BE16((ushort)numTables));
            file.AddRange(BE16((ushort)(16 << es)));
            file.AddRange(BE16((ushort)es));
            file.AddRange(BE16((ushort)(numTables * 16 - (16 << es))));
            file.AddRange(dir);
            file.AddRange(body);

            var bytes = file.ToArray();
            int headAt = pos + Offset(tables, "head");
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(headAt + 8, 4),
                unchecked(0xB1B0AFBA - Checksum(bytes)));
            return bytes;
        }

        private static int Offset(SortedDictionary<string, byte[]> tables, string tag)
        {
            int at = 0;
            foreach (var (t, d) in tables)
            {
                if (t == tag) return at;
                at += (d.Length + 3) & ~3;
            }
            throw new InvalidOperationException(tag);
        }

        private static byte[] Head(int upem)
        {
            var h = new byte[54];
            BinaryPrimitives.WriteUInt32BigEndian(h.AsSpan(0), 0x00010000);
            BinaryPrimitives.WriteUInt32BigEndian(h.AsSpan(4), 0x00010000);
            BinaryPrimitives.WriteUInt32BigEndian(h.AsSpan(12), 0x5F0F3CF5);
            BinaryPrimitives.WriteUInt16BigEndian(h.AsSpan(18), (ushort)upem);
            BinaryPrimitives.WriteUInt16BigEndian(h.AsSpan(46), 8);
            BinaryPrimitives.WriteInt16BigEndian(h.AsSpan(48), 2);
            BinaryPrimitives.WriteInt16BigEndian(h.AsSpan(50), 1); // long loca
            return h;
        }

        private static byte[] Hhea(int numberOfHMetrics)
        {
            var h = new byte[36];
            BinaryPrimitives.WriteUInt32BigEndian(h.AsSpan(0), 0x00010000);
            BinaryPrimitives.WriteInt16BigEndian(h.AsSpan(4), 800);
            BinaryPrimitives.WriteInt16BigEndian(h.AsSpan(6), -200);
            BinaryPrimitives.WriteUInt16BigEndian(h.AsSpan(10), 1000);
            BinaryPrimitives.WriteInt16BigEndian(h.AsSpan(18), 1);
            BinaryPrimitives.WriteUInt16BigEndian(h.AsSpan(34), (ushort)numberOfHMetrics);
            return h;
        }

        private static byte[] Maxp(int numGlyphs)
        {
            var m = new byte[32];
            BinaryPrimitives.WriteUInt32BigEndian(m.AsSpan(0), 0x00010000);
            BinaryPrimitives.WriteUInt16BigEndian(m.AsSpan(4), (ushort)numGlyphs);
            BinaryPrimitives.WriteUInt16BigEndian(m.AsSpan(6), 64);
            BinaryPrimitives.WriteUInt16BigEndian(m.AsSpan(8), 8);
            BinaryPrimitives.WriteUInt16BigEndian(m.AsSpan(14), 2);
            BinaryPrimitives.WriteUInt16BigEndian(m.AsSpan(28), 4);
            BinaryPrimitives.WriteUInt16BigEndian(m.AsSpan(30), 2);
            return m;
        }

        private static byte[] Post()
        {
            var p = new byte[32];
            BinaryPrimitives.WriteUInt32BigEndian(p.AsSpan(0), 0x00030000);
            return p;
        }

        private static uint Checksum(byte[] d)
        {
            uint sum = 0;
            for (int i = 0; i < d.Length; i += 4)
            {
                uint w = 0;
                for (int k = 0; k < 4; k++) w = (w << 8) | (i + k < d.Length ? d[i + k] : 0u);
                sum = unchecked(sum + w);
            }
            return sum;
        }
    }
}