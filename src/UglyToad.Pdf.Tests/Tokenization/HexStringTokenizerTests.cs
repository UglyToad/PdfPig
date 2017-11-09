namespace UglyToad.Pdf.Tests.Tokenization
{
    using Pdf.Tokenization;
    using Pdf.Tokenization.Tokens;
    using Xunit;

    public class HexStringTokenizerTests
    {
        private readonly HexStringTokenizer tokenizer = new HexStringTokenizer();

        [Theory]
        [InlineData(">not hex")]
        [InlineData("\\<not hex")]
        [InlineData("not hex")]
        [InlineData("AE1094 still not hex")]
        public void CannotTokenizeInvalidBytes(string s)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        [Theory]
        [InlineData("<00>", "\0")]
        [InlineData("<A1>", "¡")]
        public void TokenizesHexStringsCorrectly(string s, string expected)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(expected, AssertHexToken(token).Data);
        }

        private static HexToken AssertHexToken(IToken token)
        {
            Assert.NotNull(token);

            var hexToken = Assert.IsType<HexToken>(token);

            return hexToken;
        }
    }
}
