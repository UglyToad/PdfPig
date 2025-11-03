namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.Tokenization;
    using PdfPig.Tokens;

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
            new object[] {"0", 0},
            new object[] {"0003", 3},
            new object[] {"1", 1},
            new object[] {"2", 2},
            new object[] {"3", 3},
            new object[] {"4", 4},
            new object[] {"5", 5},
            new object[] {"6", 6},
            new object[] {"7", 7},
            new object[] {"8", 8},
            new object[] {"9", 9},
            new object[] {"10", 10},
            new object[] {"11", 11},
            new object[] {"29", 29},
            new object[] {"-0", 0},
            new object[] {"-0123", -123},
            new object[] {"-6.9000", -6.9},
            new object[] {"57473.3458382", 57473.3458382},
            new object[] { "123", 123},
            new object[] { "43445", 43445},
            new object[] { "+17", 17},
            new object[] { "-98", -98},
            new object[] { "34.5", 34.5},
            new object[] { "-3.62", -3.62},
            new object[] { "+123.6", 123.6},
            new object[] { "4.", 4},
            new object[] { "-.002", -0.002},
            new object[] { "0.0", 0},
            new object[] {"1.57e3", 1570},
            new object[] {"1.57e-3", 0.00157, 0.0000001},
            new object[] {"1.24e1", 12.4},
            new object[] { "1.457E2", 145.7 }
        };

        [Theory]
        [MemberData(nameof(ValidNumberTestData))]
        public void ParsesValidNumbers(string s, double expected, double? tolerance = null)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            if (tolerance.HasValue)
            {
                Assert.Equal(expected, AssertNumericToken(token).Data, tolerance: tolerance.Value);
            }
            else
            {
                Assert.Equal(expected, AssertNumericToken(token).Data);
            }
        }

        [Fact]
        public void OnlyParsesNumberPart()
        {
            var input = StringBytesTestConverter.Convert("135.6654/Type");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal(135.6654, AssertNumericToken(token).Data);

            if (tokenizer.ReadsNextByte)
                Assert.Equal('/', (char)input.Bytes.CurrentByte);
            else
                Assert.Equal('4', (char)input.Bytes.CurrentByte);
        }

        [Fact]
        public void HandlesDash()
        {
            var input = StringBytesTestConverter.Convert("-");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal(0, AssertNumericToken(token).Data);
        }

        [Fact]
        public void HandleDoubleDashedNumber()
        {
            // This is a really weird format but seen in the wild. PDF, shine on, you crazy diamond.
            var input = StringBytesTestConverter.Convert("--10.25");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal(-10.25, AssertNumericToken(token).Data);
        }

        [Fact]
        public void HandlesDot()
        {
            var input = StringBytesTestConverter.Convert(".");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal(0, AssertNumericToken(token).Data);
        }

        private static NumericToken AssertNumericToken(IToken token)
        {
            Assert.NotNull(token);
            var result = Assert.IsType<NumericToken>(token);

            return result;
        }
    }

}
