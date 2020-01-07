namespace UglyToad.PdfPig.Tests.Parser.Parts.FileStructure
{
    using System;
    using System.Linq;
    using PdfPig.Core;
    using PdfPig.CrossReference;
    using PdfPig.Parser.FileStructure;
    using PdfPig.Tokenization.Scanner;
    using Xunit;

    public class CrossReferenceTableParserTests
    {
        private readonly CrossReferenceTableParser parser = new CrossReferenceTableParser();

        [Fact]
        public void ParseNewDefaultTable()
        {
            var input = StringBytesTestConverter.Scanner(@"one xref
0 6
0000000003 65535 f
0000000090 00000 n
0000000081 00000 n
0000000000 00007 f
0000000331 00000 n
0000000409 00000 n

trailer
<< >>");

            var result = parser.Parse(input, 4, false);

            Assert.Equal(4, result.ObjectOffsets.Count);
        }

        [Fact]
        public void OffsetNotXrefThrows()
        {
            var input = GetReader("12 0 obj <<>> endobj xref");

            Action action = () => parser.Parse(input, 4, false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void OffsetXButNotXrefThrows()
        {
            var input = GetReader(@"xtable
trailer");

            Action action = () => parser.Parse(input, 0, false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void EmptyTableReturnsEmpty()
        {
            var input = GetReader(@"xref
trailer
<<>>");

            var result = parser.Parse(input, 0, false);

            Assert.Empty(result.ObjectOffsets);
        }

        [Fact]
        public void InvalidSubsectionDefinitionLenientSkips()
        {
            var input = GetReader(@"xref
ab 12
trailer
<<>>");

            var result = parser.Parse(input, 0, true);

            Assert.Empty(result.ObjectOffsets);
        }

        [Fact]
        public void InvalidSubsectionDefinitionNotLenientThrows()
        {
            var input = GetReader(@"xref
ab 12
trailer
<<>>");

            Action action = () => parser.Parse(input, 0, false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void SkipsFirstFreeLine()
        {
            var input = GetReader(@"xref
0 1
0000000000 65535 f
trailer
<<>>");

            var result = parser.Parse(input, 0, false);
            
            Assert.Empty(result.ObjectOffsets);
            Assert.Equal(0, result.Offset);
            Assert.Equal(CrossReferenceType.Table, result.Type);
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

            var result = parser.Parse(input, 0, false);
            
            Assert.Equal(2, result.ObjectOffsets.Count);

            var results = result.ObjectOffsets.Select(x => new {x.Key.ObjectNumber, x.Key.Generation, x.Value}).ToList();

            Assert.Equal(100, results[0].Value);
            Assert.Equal(1, results[0].ObjectNumber);
            Assert.Equal(0, results[0].Generation);

            Assert.Equal(200, results[1].Value);
            Assert.Equal(2, results[1].ObjectNumber);
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

            var result = parser.Parse(input, 0, false);
            
            Assert.Equal(2, result.ObjectOffsets.Count);

            var results = result.ObjectOffsets.Select(x => new { x.Key.ObjectNumber, x.Key.Generation, x.Value }).ToList();

            Assert.Equal(190, results[0].Value);
            Assert.Equal(15, results[0].ObjectNumber);
            Assert.Equal(0, results[0].Generation);

            Assert.Equal(250, results[1].Value);
            Assert.Equal(16, results[1].ObjectNumber);
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

            var result = parser.Parse(input, 0, false);
            
            Assert.Equal(2, result.ObjectOffsets.Count);

            var results = result.ObjectOffsets.Select(x => new { x.Key.ObjectNumber, x.Key.Generation, x.Value }).ToList();

            Assert.Equal(190, results[0].Value);
            Assert.Equal(15, results[0].ObjectNumber);
            Assert.Equal(0, results[0].Generation);

            Assert.Equal(250, results[1].Value);
            Assert.Equal(16, results[1].ObjectNumber);
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

            var result = parser.Parse(input, 0, false);
            
            Assert.Equal(5, result.ObjectOffsets.Count);

            var results = result.ObjectOffsets.Select(x => new { x.Key.ObjectNumber, x.Key.Generation, x.Value }).ToList();

            Assert.Equal(100, results[0].Value);
            Assert.Equal(1, results[0].ObjectNumber);
            Assert.Equal(0, results[0].Generation);

            Assert.Equal(200, results[1].Value);
            Assert.Equal(2, results[1].ObjectNumber);
            Assert.Equal(5, results[1].Generation);

            Assert.Equal(230, results[2].Value);
            Assert.Equal(3, results[2].ObjectNumber);
            Assert.Equal(5, results[2].Generation);

            Assert.Equal(190, results[3].Value);
            Assert.Equal(15, results[3].ObjectNumber);
            Assert.Equal(7, results[3].Generation);

            Assert.Equal(250, results[4].Value);
            Assert.Equal(16, results[4].ObjectNumber);
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

            Action action = () => parser.Parse(input, 0, false);

            Assert.Throws<PdfDocumentFormatException>(action);
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

            Action action = () => parser.Parse(input, 0, false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void ShortLineInTableReturnsThrows()
        {
            var input = GetReader(@"xref
15 2
019 n
0000000250 00032 n
trailer
<<>>");

            Action action = () => parser.Parse(input, 0, false);

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void SkipsBlankLinesPrecedingTrailer()
        {
            var input = GetReader(@"xref
15 2
0000000190 00000 n
0000000250 00032 n

trailer
<<>>");

            var result = parser.Parse(input, 0, false);
            
            Assert.Equal(2, result.ObjectOffsets.Count);
        }

        private static CoreTokenScanner GetReader(string input)
        {
            return StringBytesTestConverter.Scanner(input);
        }
    }
}
