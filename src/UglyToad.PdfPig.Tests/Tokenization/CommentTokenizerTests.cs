namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.Tokenization;
    using PdfPig.Tokenization.Tokens;
    using Xunit;

    public class CommentTokenizerTests
    {
        private readonly CommentTokenizer commentTokenizer = new CommentTokenizer();

        [Theory]
        [InlineData("(%not a comment)")]
        [InlineData("\\%not a comment)")]
        [InlineData("‰")]
        public void InvalidFirstCharacter_ReturnsFalse(string s)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = commentTokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        [Theory]
        [InlineData(@"%Resource-CMAP
%AnotherComment", "Resource-CMAP")]
        [InlineData("%%PDF 1.5", "%PDF 1.5")]
        [InlineData(@"% comment {/%) blah blah blah
            123", " comment {/%) blah blah blah")]
        [InlineData("%comment\rNot comment", "comment")]
        [InlineData("%comment\r\nNot comment", "comment")]
        [InlineData("%comment\nNot comment", "comment")]
        public void ParsesComment(string s, string expected)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = commentTokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var comment = Assert.IsType<CommentToken>(token);

            Assert.Equal(expected, comment.Data);
        }
    }
}
