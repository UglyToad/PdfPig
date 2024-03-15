namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.Tokenization;
    using PdfPig.Tokens;

    public class HexTokenizerTests
    {
        private readonly HexTokenizer tokenizer = new HexTokenizer();

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
        [InlineData("<00>", "")]
        [InlineData("<A1>", "¡")]
        public void TokenizesHexStringsCorrectly(string s, string expected)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(expected, AssertHexToken(token).Data);
        }

        [Theory]
        [InlineData("<FEFF004C0069006200720065004F0066006600690063006500200036002E0031>", "LibreOffice 6.1")]
        [InlineData("<FEFF30533093306B3061306F4E16754C>", "こんにちは世界")]
        public void HandlesUtf16Strings(string s, string expected)
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
