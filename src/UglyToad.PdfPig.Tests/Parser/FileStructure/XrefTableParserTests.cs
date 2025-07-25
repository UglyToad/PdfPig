namespace UglyToad.PdfPig.Tests.Parser.FileStructure;

using Logging;
using PdfPig.Core;
using PdfPig.Parser.FileStructure;
using PdfPig.Tokens;

public class XrefTableParserTests
{
    [Fact]
    public void ParseSimpleXref()
    {
        const string input =
            """
            xref
            12 3
            0000000000 65535 f 
            0000000443 00000 n 
            0000000576 00000 n
            trailer
            << /Size 323 >>
            """;

        var table = GetTableForString(input);

        AssertObjectsMatch(table,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(13, 0), 443 },
                { new IndirectReference(14, 0), 576 },
            });

        Assert.Equal(table.Offset, 0);

        Assert.NotNull(table.Dictionary);
    }

    [Fact]
    public void ParseSimpleXrefWithComments()
    {
        const string input =
            """
            xref
            12 2
            0000000000 65535 f % Hello
            0000000443 00000 n % comments are very bad and not allowed 0000000576 00000 n
            trailer
            << /Size 323 >>
            """;

        var table = GetTableForString(input);

        AssertObjectsMatch(table,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(13, 0), 443 }
            });

        Assert.Equal(table.Offset, 0);

        Assert.NotNull(table.Dictionary);
    }

    [Fact]
    public void ParseSimpleXrefFollowedByObject()
    {
        const string input =
            """
            xref
            19 3
            0000000000 65535 f 
            23255 00000 n 
            0000002122 00000 n
            4 0 obj
            12
            endobj
            """;

        var table = GetTableForString(input);

        AssertObjectsMatch(table,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(20, 0), 23255},
                { new IndirectReference(21, 0), 2122},
            });

        Assert.Equal(table.Offset, 0);

        Assert.Null(table.Dictionary);
    }

    [Fact]
    public void ParseXrefMissingLineBreaks()
    {
        const string input = "xref 10 2 000000 65535 f 00013772 10 n << /type /beans >>";

        var table = GetTableForString(input);

        AssertObjectsMatch(table,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(11, 10), 13772 }
            });

        Assert.Null(table.Dictionary);
    }

    [Fact]
    public void ParseSimpleXrefMissingNewline()
    {
        const string input =
            """
            xref10 3
            0000000000 65535 f 
            0000000443 00000 n 
            0000000576 00000 n
            trailer
            << /Type /Arg /Prev 2344 >>
            """;

        var table = GetTableForString(input);

        AssertObjectsMatch(table,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(11, 0), 443 },
                { new IndirectReference(12, 0), 576 },
            });

        Assert.Equal(table.Offset, 0);
        Assert.NotNull(table.Dictionary);
    }

    [Fact]
    public void ParsePdfSpecXref()
    {
        const string input =
            """
            xref
            0 1
            0000000000 65535 f
            3 1
            0000025325 00000 n
            23 2
            0000025518 00002 n
            0000025635 00000 n
            30 1
            0000025777 00000 n
            """;

        var table = GetTableForString(input);

        AssertObjectsMatch(table,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(3, 0), 25325 },
                { new IndirectReference(23, 2), 25518 },
                { new IndirectReference(24, 0), 25635 },
                { new IndirectReference(30, 0), 25777 },
            });

        Assert.Null(table.Dictionary);
    }

    [Fact]
    public void ParseTrailerDictionaryMissingNewline()
    {
        const string input =
            """
            xref
            0 2
            0000000000 65535 f
            0000025325 00000 n trailer<< /Size 123>> %%EOF
            """;

        var table = GetTableForString(input);

        Assert.NotNull(table.Dictionary);
        Assert.Equal(new NumericToken(123), table.Dictionary.Data["Size"]);
    }

    [Theory]
    [InlineData(
        """
        wibbly290 243543
        434
        """),
    InlineData(
        """
        xref 0 10 trailer 33 5
        """)]
    [InlineData(
        """
        xref 100 0
        10 5 n
        100 45 n
        xref
        trailer
        """)]
    public void ParseCorruptXrefs(string xref)
    {
        var table = GetTableForString(xref);

        Assert.Null(table);
    }

    [Fact]
    public void ParseTestDocumentExample()
    {
        const string input =
            """
            xref0 40
            0000000000 65535 f 
            0000000015 00000 n 
            0000000085 00000 n 
            0000000371 00000 n 
            0000000658 00000 n 
            0000000920 00000 n 
            0000000969 00000 n 
            0000001096 00000 n 
            0000001448 00000 n 
            0000002162 00000 n 
            0000005207 00000 n 
            0000005316 00000 n 
            0000005543 00000 n 
            0000056503 00000 n 
            0000075543 00000 n 
            0000075968 00000 n 
            0000076313 00000 n 
            0000077592 00000 n 
            0000077721 00000 n 
            0000078076 00000 n 
            0000078846 00000 n 
            0000082166 00000 n 
            0000082275 00000 n 
            0000082501 00000 n 
            0000120640 00000 n 
            0000122623 00000 n 
            0000124952 00000 n 
            0000138582 00000 n 
            0000139875 00000 n 
            0000141303 00000 n 
            0000142686 00000 n 
            0000143385 00000 n 
            0000144099 00000 n 
            0000144227 00000 n 
            0000144584 00000 n 
            0000145335 00000 n 
            0000148764 00000 n 
            0000148873 00000 n 
            0000149022 00000 n 
            0000152670 00000 n 
            trailer
            <<
            /Root 5 0 R
            /Size 40
            >>
            startxref
            174834
            %%EOF
            """;

        var table = GetTableForString(input);

        Assert.NotNull(table);

        Assert.Equal(39, table.ObjectOffsets.Count);
    }
    [Fact]
    public void ParseNewDefaultTable()
    {
        var input = StringBytesTestConverter.Scanner(
            """
            one xref
            0 6
            0000000003 65535 f
            0000000090 00000 n
            0000000081 00000 n
            0000000000 00007 f
            0000000331 00000 n
            0000000409 00000 n

            trailer
            << >>
            """);

        var result = XrefTableParser.TryReadTableAtOffset(4, input.bytes, input.scanner, new TestingLog());

        Assert.NotNull(result);
        Assert.Equal(4, result.ObjectOffsets.Count);
    }

    [Fact]
    public void OffsetNotXrefThrows()
    {
        var result = Parse("12 0 obj <<>> endobj xref");

        Assert.Null(result);
    }

    [Fact]
    public void OffsetXButNotXrefThrows()
    {
        var result = Parse(
            """
            xtable
            trailer
            """);

        Assert.Null(result);
    }

    [Fact]
    public void EmptyTableReturnsEmpty()
    {
        var result = Parse(
            """
            xref
            trailer
            <<>>
            """);

        Assert.NotNull(result);
        Assert.NotNull(result.Dictionary);
        Assert.Empty(result.ObjectOffsets);
    }

    [Fact]
    public void InvalidSubsectionDefinitionLenientSkips()
    {
        var result = Parse(
            """
            xref
            ab 12
            trailer
            <<>>
            """);

        Assert.Null(result);
    }

    [Fact]
    public void SkipsFirstFreeLine()
    {
        var result = Parse(
            """
            xref
            0 1
            0000000000 65535 f
            trailer
            <<>>
            """);

        Assert.NotNull(result);
        Assert.NotNull(result.Dictionary);
        Assert.Empty(result.ObjectOffsets);
    }

    [Fact]
    public void ReadsEntries()
    {
        var result = Parse(
            """
            xref
            0 3
            0000000000 65535 f
            0000000100 00000 n
            0000000200 00005 n
            trailer
            <<>>
            """);

        AssertObjectsMatch(result,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(1, 0), 100 },
                { new IndirectReference(2, 5), 200 },
            });

        Assert.NotNull(result.Dictionary);
    }

    [Fact]
    public void ReadsEntriesOffsetFirstNumber()
    {
        var result = Parse(
            """
            xref
            15 2
            0000000190 00000 n
            0000000250 00032 n
            trailer
            <<>>
            """);

        AssertObjectsMatch(result,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(15, 0), 190 },
                { new IndirectReference(16, 32), 250 },
            });
    }

    [Fact]
    public void ReadsEntriesSkippingBlankLine()
    {
        var result = Parse(
            """
            xref
            15 2
            0000000190 00000 n

            0000000250 00032 n
            trailer
            <<>>
            """);

        AssertObjectsMatch(result,
            new Dictionary<IndirectReference, long>
            {
                {new IndirectReference(15, 0), 190},
                {new IndirectReference(16, 32), 250},
            });
    }

    [Fact]
    public void ReadsEntriesFromMultipleSubsections()
    {
        var result = Parse(
            """
            xref
            0 4
            0000000000 65535 f
            0000000100 00000 n
            0000000200 00005 n
            0000000230 00005 n
            15 2
            0000000190 00007 n
            0000000250 00032 n
            trailer
            <<>>
            """);

        AssertObjectsMatch(result,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(1, 0), 100 },
                { new IndirectReference(2, 5), 200 },
                { new IndirectReference(3, 5), 230 },
                { new IndirectReference(15, 7), 190 },
                { new IndirectReference(16, 32), 250 },
            });
    }

    [Fact]
    public void EntryPointingAtOffsetInTableDoesNotThrow()
    {
        var result = Parse(
            """
            xref
            0 2
            0000000000 65535 f
            0000000010 00000 n
            trailer
            <<>>
            """);

        AssertObjectsMatch(result,
            new Dictionary<IndirectReference, long>
            {
                { new IndirectReference(1, 0), 10 }
            });
    }

    [Fact]
    public void EntryWithInvalidFormatThrows()
    {
        var result = Parse(
            """
            xref
            0 22
            0000000000 65535 f
            0000aa0010 00000 n
            trailer
            <<>>
            """);

        Assert.Null(result);
    }

    [Fact]
    public void ShortLineInTableReturnsThrows()
    {
        var result = Parse(
            """
            xref
            15 2
            019 n
            0000000250 00032 n
            trailer
            <<>>
            """);

        Assert.Null(result);
    }

    [Fact]
    public void SkipsBlankLinesPrecedingTrailer()
    {
        var result = Parse(
            """
            xref
            15 2
            0000000190 00000 n
            0000000250 00032 n

            trailer
            <<>>
            """);

        Assert.Equal(2, result.ObjectOffsets.Count);
    }

    [Fact]
    public void ParseEntriesAfterDeclaredCountIfLenient()
    {
        const string data = 
            """
            xref
            0 5
            0000000003 65535 f
            0000000090 00000 n
            0000000081 00000 n
            0000000223 00000 n
            0000000331 00000 n
            0000000127 00000 n
            0000000409 00000 f
            0000000418 00000 n

            trailer
            << >>
            """;

        var result = GetTableForString(data);

        Assert.Equal(6, result.ObjectOffsets.Count);
    }

    [Fact]
    public void ParsesMissingWhitespaceAfterXref()
    {
        var data = 
            """
            xref15 2
            0000000190 00000 n
            0000000250 00032 n

            trailer
            <<>>
            """;

        var result = GetTableForString(data);

        Assert.Equal(2, result.ObjectOffsets.Count);
    }

    private static XrefTable Parse(string str)
    {
        var input = StringBytesTestConverter.Scanner(str);

        return XrefTableParser.TryReadTableAtOffset(
            0,
            input.bytes,
            input.scanner,
            new TestingLog());
    }

    private static void AssertObjectsMatch(
        XrefTable table,
        Dictionary<IndirectReference, long> offsets)
    {
        Assert.NotNull(table);

        Assert.Equal(table.ObjectOffsets.Count, offsets.Count);
        foreach (var offset in offsets)
        {
            Assert.True(table.ObjectOffsets.TryGetValue(offset.Key, out var actual));

            Assert.Equal(offset.Value, actual);
        }
    }

    private static XrefTable GetTableForString(string s)
    {
        var ib = StringBytesTestConverter.Scanner(s);
        var log = new NoOpLog();

        var table = XrefTableParser.TryReadTableAtOffset(
            0,
            ib.bytes,
            ib.scanner,
            log);

        return table;
    }
}
