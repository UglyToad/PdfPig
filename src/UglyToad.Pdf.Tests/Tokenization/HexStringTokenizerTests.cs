namespace UglyToad.Pdf.Tests.Tokenization
{
    using Pdf.Tokenization;
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
    }
}
