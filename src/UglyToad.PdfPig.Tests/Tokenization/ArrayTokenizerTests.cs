namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.Tokenization;
    using PdfPig.Tokens;

    public class ArrayTokenizerTests
    {
        private readonly ArrayTokenizer tokenizer = new ArrayTokenizer(true);

        [Theory]
        [InlineData("]")]
        [InlineData("<")]
        [InlineData(" [")]
        [InlineData("a")]
        [InlineData("\0")]
        public void InvalidFirstCharacter_ReturnsFalse(string s)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        public static IEnumerable<object[]> SingleElementTestData => new[]
        {
            new object[] {"[12]", 12.0},
            new object[] {"[ 12 ]", 12.0},
            new object[] {@"[ 
2948344 ]", 2948344.0},
            new object[] { "[(Bertrand)  \t]", "Bertrand" },
            new object[] { "[ <AE>\r\n]", "®" },
        };

        [Theory]
        [MemberData(nameof(SingleElementTestData))]
        public void SingleElementArray(string s, object dataValue)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var array = AssertArrayToken(token);

            Assert.Single(array.Data);

            Assert.Equal(dataValue, ((dynamic)token).Data[0].Data);
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("[ ]")]
        [InlineData("[\r\n\r\n\t]")]
        public void EmptyArray(string s)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var array = AssertArrayToken(token);

            Assert.Empty(array.Data);
        }

        [Fact]
        public void NestedArray()
        {
            const string s = "[ 12 +10.453 /Fonts [ /F1 /F3 ] (Moreover) ]";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var array = AssertArrayToken(token);

            Assert.Equal(12, AssertDataToken<NumericToken, double>(0, array).Data);
            Assert.Equal(10.453, AssertDataToken<NumericToken, double>(1, array).Data);
            Assert.Equal(NameToken.Create("Fonts"), AssertDataToken<NameToken, string>(2, array).Data);

            var inner = AssertArrayToken(array.Data[3]);

            Assert.Equal(NameToken.Create("F1"), AssertDataToken<NameToken, string>(0, inner).Data);
            Assert.Equal(NameToken.Create("F3"), AssertDataToken<NameToken, string>(1, inner).Data);

            Assert.Equal("Moreover", AssertDataToken<StringToken, string>(4, array).Data);
        }

        [Fact]
        public void ManyNestedArrays()
        {
            const string s = "[ /Bounds [ [19 -69.] [7 64.625]] (More) [[[15]]]]";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var array = AssertArrayToken(token);

            Assert.Equal(NameToken.Create("Bounds"), AssertDataToken<NameToken, string>(0, array).Data);

            var firstInner = AssertArrayToken(array.Data[1]);

            var firstFirstInner = AssertArrayToken(firstInner.Data[0]);

            Assert.Equal(19, AssertDataToken<NumericToken, double>(0, firstFirstInner).Data);
            Assert.Equal(-69, AssertDataToken<NumericToken, double>(1, firstFirstInner).Data);

            var secondFirstInner = AssertArrayToken(firstInner.Data[1]);

            Assert.Equal(7, AssertDataToken<NumericToken, double>(0, secondFirstInner).Data);
            Assert.Equal(64.625, AssertDataToken<NumericToken, double>(1, secondFirstInner).Data);

            Assert.Equal("More", AssertDataToken<StringToken, string>(2, array).Data);

            var secondInner = AssertArrayToken(array.Data[3]);

            var firstSecondInner = AssertArrayToken(secondInner.Data[0]);

            var firstFirstSecondInner = AssertArrayToken(firstSecondInner.Data[0]);

            Assert.Equal(15, AssertDataToken<NumericToken, double>(0, firstFirstSecondInner).Data);
        }

        [Fact]
        public void SpecificationExampleArray()
        {
            const string s = "[549 3.14 false (Ralph) /SomeName]";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var array = AssertArrayToken(token);

            Assert.Equal(549, AssertDataToken<NumericToken, double>(0, array).Data);
            Assert.Equal(3.14, AssertDataToken<NumericToken, double>(1, array).Data);
            Assert.False(AssertDataToken<BooleanToken, bool>(2, array).Data);
            Assert.Equal("Ralph", AssertDataToken<StringToken, string>(3, array).Data);
            Assert.Equal(NameToken.Create("SomeName"), AssertDataToken<NameToken, string>(4, array).Data);
        }

        private static ArrayToken AssertArrayToken(IToken token)
        {
            Assert.NotNull(token);

            var result = Assert.IsType<ArrayToken>(token);

            return result;
        }

        private static T AssertDataToken<T, TData>(int index, ArrayToken array) where T : IDataToken<TData>
        {
            Assert.True(array.Data.Count > index);

            var result = Assert.IsType<T>(array.Data[index]);

            return result;
        }
    }
}
