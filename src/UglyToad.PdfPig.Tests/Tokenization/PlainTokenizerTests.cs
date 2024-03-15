namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.Tokenization;
    using PdfPig.Tokens;

    public class PlainTokenizerTests
    {
        private readonly PlainTokenizer tokenizer = new PlainTokenizer();

        [Fact]
        public void TextNullReturnsNullToken()
        {
            var input = StringBytesTestConverter.Convert("null ");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(NullToken.Instance, token);
        }

        [Fact]
        public void TryTokenizeWhitespaceFalse()
        {
            var input = StringBytesTestConverter.Convert("    something");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.False(result);

            Assert.Null(token);
        }
    }
}
