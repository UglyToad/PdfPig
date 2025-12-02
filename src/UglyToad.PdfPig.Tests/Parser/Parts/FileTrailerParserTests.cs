namespace UglyToad.PdfPig.Tests.Parser.Parts;

using PdfPig.Core;
using PdfPig.Parser.FileStructure;
using PdfPig.Tokenization.Scanner;

public class FirstPassParserStartXrefTests
{
    [Fact]
    public void FindsCompliantStartXref()
    {
        var input = StringBytesTestConverter.Convert(
            """
            sta455%r endstream
            endobj

            12 0 obj
            1234  %eof
            endobj

            startxref
                456

            %%EOF
            """,
            false);

        var result = FirstPassParser.GetFirstCrossReferenceOffset(
            input.Bytes,
            new CoreTokenScanner(input.Bytes, true, new StackDepthGuard(256)),
            new TestingLog());

        Assert.Equal(456, result.StartXRefDeclaredOffset);
    }

    [Fact]
    public void IncludesStartXrefFollowingEndOfFile()
    {
        var input = StringBytesTestConverter.Convert(
            """
            11 0 obj
            << /Type/Something /W[12 0 5 6] >>
            endobj

            12 0 obj
            1234  %eof
            endobj

            startxref
                1384733

            %%EOF

            % I decided to put some nonsense here:
            % because I could hahaha
            startxref
            17
            """,
            false);

        var result = FirstPassParser.GetFirstCrossReferenceOffset(
            input.Bytes,
            new CoreTokenScanner(input.Bytes, true, new StackDepthGuard(256)),
            new TestingLog());

        Assert.Equal(17, result.StartXRefDeclaredOffset);
    }

    [Fact]
    public void MissingStartXrefThrows()
    {
        var input = StringBytesTestConverter.Convert(
            """
            11 0 obj
            << /Type/Something /W[12 0 5 6] >>
            endobj

            12 0 obj
            1234  %eof
            endobj

            startref
                1384733

            %%EOF

            % I decided to put some nonsense here:
            % because I could hahaha
            start_rexf
            17
            """,
            false);


        var result = FirstPassParser.GetFirstCrossReferenceOffset(
            input.Bytes,
            new CoreTokenScanner(input.Bytes, true, new StackDepthGuard(256)),
            new TestingLog());

        Assert.Equal(1384733, result.StartXRefDeclaredOffset);
    }

    [Fact]
    public void BadInputBytesReturnsNull()
    {
        var input = StringBytesTestConverter.Convert("11 0 obj", false);

        var result = FirstPassParser.GetFirstCrossReferenceOffset(
            input.Bytes,
            new CoreTokenScanner(input.Bytes, true, new StackDepthGuard(256)),
            new TestingLog());

        Assert.Null(result.StartXRefDeclaredOffset);
        Assert.Null(result.StartXRefOperatorToken);
    }

    [Fact]
    public void InvalidTokensAfterStartXrefReturnsNull()
    {
        var input = StringBytesTestConverter.Convert(
            """
            11 0 obj
                    << /Type/Font >>
            endobj

            startxref 
            << /Why (am i here?) >> 69
            %EOF
            """,
            false);

        var result = FirstPassParser.GetFirstCrossReferenceOffset(
            input.Bytes,
            new CoreTokenScanner(input.Bytes, true, new StackDepthGuard(256)),
            new TestingLog());

        Assert.Null(result.StartXRefDeclaredOffset);
        Assert.NotNull(result.StartXRefOperatorToken);
    }

    [Fact]
    public void MissingNumericAfterStartXrefReturnsNull()
    {
        var input = StringBytesTestConverter.Convert(
            """
            1 0 obj
            << /Type/Font >>
            endobj

            startxref 
            """, false);

        var result = FirstPassParser.GetFirstCrossReferenceOffset(
            input.Bytes,
            new CoreTokenScanner(input.Bytes, true, new StackDepthGuard(256)),
            new TestingLog());

        Assert.Null(result.StartXRefDeclaredOffset);
        Assert.NotNull(result.StartXRefOperatorToken);
    }

    [Fact]
    public void TakesLastStartXrefPrecedingEndOfFile()
    {
        var input = StringBytesTestConverter.Convert(
            """
            11 0 obj
            << /Type/Something /W[12 0 5 6] >>
            endobj

            12 0 obj
            1234  %eof
            endobj

            startxref
                1384733

            %actually I changed my mind

            startxref
                     1274665676543

            %%EOF
            """,
            false);

        var result = FirstPassParser.GetFirstCrossReferenceOffset(
            input.Bytes,
            new CoreTokenScanner(input.Bytes, true, new StackDepthGuard(256)),
            new TestingLog());

        Assert.Equal(1274665676543, result.StartXRefDeclaredOffset);
        Assert.NotNull(result.StartXRefOperatorToken);
    }

    [Fact]
    public void CanReadStartXrefIfCommentsPresent()
    {
        var input = StringBytesTestConverter.Convert(
            """

            startxref %Commented here
                57695

            %%EOF
            """,
            false);

        var result = FirstPassParser.GetFirstCrossReferenceOffset(
            input.Bytes,
            new CoreTokenScanner(input.Bytes, true, new StackDepthGuard(256)),
            new TestingLog());

        Assert.Equal(57695, result.StartXRefDeclaredOffset);
        Assert.NotNull(result.StartXRefOperatorToken);
    }
}