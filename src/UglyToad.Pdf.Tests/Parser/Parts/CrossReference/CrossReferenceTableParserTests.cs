namespace UglyToad.Pdf.Tests.Parser.Parts.CrossReference
{
    using System;
    using System.Linq;
    using IO;
    using Pdf.Cos;
    using Pdf.Parser.Parts.CrossReference;
    using Pdf.Util;
    using Xunit;

    public class CrossReferenceTableParserTests
    {
        private readonly CosObjectPool objectPool = new CosObjectPool();

        private readonly CrossReferenceTableParser parser = new CrossReferenceTableParser(new TestingLog(),
            new TestDictionaryParser(),
            new TestBaseParser());

        [Fact]
        public void OffsetNotXrefFalse()
        {
            var input = GetReader("12 0 obj <<>> endobj xref");

            var result = parser.TryParse(input, 4, false, objectPool, out var _);

            Assert.False(result);
        }

        [Fact]
        public void OffsetXButNotXrefFalse()
        {
            var input = GetReader(@"xtable
trailer");

            var result = parser.TryParse(input, 0, false, objectPool, out var _);

            Assert.False(result);
        }

        [Fact]
        public void EmptyTableFalse()
        {
            var input = GetReader(@"xref
trailer");

            var result = parser.TryParse(input, 0, false, objectPool, out var _);

            Assert.False(result);
        }

        [Fact]
        public void InvalidSubsectionDefinitionLenientTrue()
        {
            var input = GetReader(@"xref
ab 12
trailer
<<>>");

            var result = parser.TryParse(input, 0, true, objectPool, out var _);

            Assert.True(result);
        }

        [Fact]
        public void InvalidSubsectionDefinitionNotLenientFalse()
        {
            var input = GetReader(@"xref
ab 12
trailer
<<>>");

            var result = parser.TryParse(input, 0, false, objectPool, out var _);

            Assert.False(result);
        }

        [Fact]
        public void SkipsFirstFreeLine()
        {
            var input = GetReader(@"xref
0 1
0000000000 65535 f
trailer
<<>>");

            var result = parser.TryParse(input, 0, false, objectPool, out var table);

            Assert.True(result);

            var built = table.AsCrossReferenceTablePart();

            Assert.Empty(built.ObjectOffsets);
            Assert.Equal(0, built.Offset);
            Assert.Equal(CrossReferenceType.Table, built.Type);
        }

        [Fact]
        public void ReadsEntries()
        {
            var input = GetReader(@"xref
0 3
0000000000 65535 f
0000000100 00000 n
0000000200 00005 n
trailer
<<>>");

            var result = parser.TryParse(input, 0, false, objectPool, out var table);

            Assert.True(result);

            var built = table.AsCrossReferenceTablePart();

            Assert.Equal(2, built.ObjectOffsets.Count);

            var results = built.ObjectOffsets.Select(x => new {x.Key.Number, x.Key.Generation, x.Value}).ToList();

            Assert.Equal(100, results[0].Value);
            Assert.Equal(1, results[0].Number);
            Assert.Equal(0, results[0].Generation);

            Assert.Equal(200, results[1].Value);
            Assert.Equal(2, results[1].Number);
            Assert.Equal(5, results[1].Generation);
        }

        [Fact]
        public void ReadsEntriesOffsetFirstNumber()
        {
            var input = GetReader(@"xref
15 2
0000000190 00000 n
0000000250 00032 n
trailer
<<>>");

            var result = parser.TryParse(input, 0, false, objectPool, out var table);

            Assert.True(result);

            var built = table.AsCrossReferenceTablePart();

            Assert.Equal(2, built.ObjectOffsets.Count);

            var results = built.ObjectOffsets.Select(x => new { x.Key.Number, x.Key.Generation, x.Value }).ToList();

            Assert.Equal(190, results[0].Value);
            Assert.Equal(15, results[0].Number);
            Assert.Equal(0, results[0].Generation);

            Assert.Equal(250, results[1].Value);
            Assert.Equal(16, results[1].Number);
            Assert.Equal(32, results[1].Generation);
        }

        [Fact]
        public void ReadsEntriesSkippingBlankLine()
        {
            var input = GetReader(@"xref
15 2
0000000190 00000 n

0000000250 00032 n
trailer
<<>>");

            var result = parser.TryParse(input, 0, false, objectPool, out var table);

            Assert.True(result);

            var built = table.AsCrossReferenceTablePart();

            Assert.Equal(2, built.ObjectOffsets.Count);

            var results = built.ObjectOffsets.Select(x => new { x.Key.Number, x.Key.Generation, x.Value }).ToList();

            Assert.Equal(190, results[0].Value);
            Assert.Equal(15, results[0].Number);
            Assert.Equal(0, results[0].Generation);

            Assert.Equal(250, results[1].Value);
            Assert.Equal(16, results[1].Number);
            Assert.Equal(32, results[1].Generation);
        }

        [Fact]
        public void ReadsEntriesFromMultipleSubsections()
        {
            var input = GetReader(@"xref
0 4
0000000000 65535 f
0000000100 00000 n
0000000200 00005 n
0000000230 00005 n
15 2
0000000190 00007 n
0000000250 00032 n
trailer
<<>>");

            var result = parser.TryParse(input, 0, false, objectPool, out var table);

            Assert.True(result);

            var built = table.AsCrossReferenceTablePart();

            Assert.Equal(5, built.ObjectOffsets.Count);

            var results = built.ObjectOffsets.Select(x => new { x.Key.Number, x.Key.Generation, x.Value }).ToList();

            Assert.Equal(100, results[0].Value);
            Assert.Equal(1, results[0].Number);
            Assert.Equal(0, results[0].Generation);

            Assert.Equal(200, results[1].Value);
            Assert.Equal(2, results[1].Number);
            Assert.Equal(5, results[1].Generation);

            Assert.Equal(230, results[2].Value);
            Assert.Equal(3, results[2].Number);
            Assert.Equal(5, results[2].Generation);

            Assert.Equal(190, results[3].Value);
            Assert.Equal(15, results[3].Number);
            Assert.Equal(7, results[3].Generation);

            Assert.Equal(250, results[4].Value);
            Assert.Equal(16, results[4].Number);
            Assert.Equal(32, results[4].Generation);
        }

        [Fact]
        public void EntryPointingAtOffsetInTableThrows()
        {
            var input = GetReader(@"xref
0 2
0000000000 65535 f
0000000010 00000 n
trailer
<<>>");

            Action action = () => parser.TryParse(input, 0, false, objectPool, out var _);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void EntryWithInvalidFormatThrows()
        {
            var input = GetReader(@"xref
0 22
0000000000 65535 f
0000aa0010 00000 n
trailer
<<>>");

            Action action = () => parser.TryParse(input, 0, false, objectPool, out var _);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void ShortLineInTableReturnsFalse()
        {
            var input = GetReader(@"xref
15 2
000000019000000 n
0000000250 00032 n
trailer
<<>>");

            var result = parser.TryParse(input, 0, false, objectPool, out var table);

            Assert.False(result);
        }

        private static IRandomAccessRead GetReader(string input)
        {
            return new RandomAccessBuffer(OtherEncodings.StringAsLatin1Bytes(input));
        }
    }
}
