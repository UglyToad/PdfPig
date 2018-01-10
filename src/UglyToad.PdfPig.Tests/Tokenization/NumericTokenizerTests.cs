namespace UglyToad.PdfPig.Tests.Tokenization
{
    using System.Collections.Generic;
    using PdfPig.Tokenization;
    using PdfPig.Tokenization.Tokens;
    using Xunit;

    public class NumericTokenizerTests
    {
        private readonly NumericTokenizer tokenizer = new NumericTokenizer();

        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("A")]
        [InlineData("|")]
        [InlineData("z")]
        [InlineData("e")]
        [InlineData("E")]
        [InlineData("\n")]
        public void FirstByteInvalid_ReturnsFalse(string s)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        public static IEnumerable<object[]> ValidNumberTestData => new []
        {
            new object[] {"0", 0m},
            new object[] {"1", 1m},
            new object[] {"2", 2m},
            new object[] {"3", 3m},
            new object[] {"4", 4m},
            new object[] {"5", 5m},
            new object[] {"6", 6m},
            new object[] {"7", 7m},
            new object[] {"8", 8m},
            new object[] {"9", 9m},
            new object[] {"10", 10m},
            new object[] {"11", 11m},
            new object[] {"29", 29m},
            new object[] {"-0", 0m},
            new object[] {"-0123", -123m},
            new object[] {"-6.9000", -6.9m},
            new object[] {"57473.3458382", 57473.3458382m},
            new object[] { "123", 123m},
            new object[] { "43445", 43445m},
            new object[] { "+17", 17m},
            new object[] { "-98", -98m},
            new object[] { "34.5", 34.5m},
            new object[] { "-3.62", -3.62m},
            new object[] { "+123.6", 123.6m},
            new object[] { "4.", 4m},
            new object[] { "-.002", -0.002m},
            new object[] { "0.0", 0m},
            new object[] {"1.57e3", 1570m}
        };

        [Theory]
        [MemberData(nameof(ValidNumberTestData))]
        public void ParsesValidNumbers(string s, decimal expected)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal(expected, AssertNumericToken(token).Data);
        }

        [Fact]
        public void OnlyParsesNumberPart()
        {
            var input = StringBytesTestConverter.Convert("135.6654/Type");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal(135.6654m, AssertNumericToken(token).Data);

            Assert.Equal('/', (char)input.Bytes.CurrentByte);
        }

        [Fact]
        public void HandlesDash()
        {
            var input = StringBytesTestConverter.Convert("-");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal(0m, AssertNumericToken(token).Data);
        }

        [Fact]
        public void HandlesDot()
        {
            var input = StringBytesTestConverter.Convert(".");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal(0m, AssertNumericToken(token).Data);
        }

        private static NumericToken AssertNumericToken(IToken token)
        {
            Assert.NotNull(token);
            var result = Assert.IsType<NumericToken>(token);

            return result;
        }
    }

}
