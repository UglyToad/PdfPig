namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.Tokenization;
    using PdfPig.Tokens;
    using Xunit;

    public class EndOfLineTokenizerTests
    {
        private readonly EndOfLineTokenizer tokenizer = new EndOfLineTokenizer();

        [Fact]
        public void CurrentByteIsNotEndOfLineFalse()
        {
            var input = StringBytesTestConverter.Convert("\r something \n", false);

            var result = tokenizer.TryTokenize((byte)'\0', input.Bytes, out var _);

            Assert.False(result);
        }

        [Fact]
        public void CurrentByteIsCarriageReturnTrue()
        {
            var input = StringBytesTestConverter.Convert("\r", false);

            var result = tokenizer.TryTokenize((byte)'\r', input.Bytes, out var token);

            Assert.True(result);

            Assert.Same(EndOfLineToken.Token, token);
        }

        [Fact]
        public void CurrentByteIsEndOfLineTrue()
        {
            var input = StringBytesTestConverter.Convert("\n", false);

            var result = tokenizer.TryTokenize((byte)'\n', input.Bytes, out var token);

            Assert.True(result);

            Assert.Same(EndOfLineToken.Token, token);
        }
    }
}
