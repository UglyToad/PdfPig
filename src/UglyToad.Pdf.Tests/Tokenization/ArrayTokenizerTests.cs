namespace UglyToad.Pdf.Tests.Tokenization
{
    using System.Collections.Generic;
    using Pdf.Cos;
    using Pdf.Tokenization;
    using Pdf.Tokenization.Tokens;
    using Xunit;

    public class ArrayTokenizerTests
    {
        private readonly ArrayTokenizer tokenizer = new ArrayTokenizer();

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
            new object[] {"[12]", 12m},
            new object[] {"[ 12 ]", 12m},
            new object[] {@"[ 
2948344 ]", 2948344m},
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

            Assert.Equal(1, array.Data.Count);

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

            Assert.Equal(12m, AssertDataToken<NumericToken, decimal>(0, array).Data);
            Assert.Equal(10.453m, AssertDataToken<NumericToken, decimal>(1, array).Data);
            Assert.Equal(CosName.Create("Fonts"), AssertDataToken<NameToken, CosName>(2, array).Data);

            var inner = AssertArrayToken(array.Data[3]);

            Assert.Equal(CosName.Create("F1"), AssertDataToken<NameToken, CosName>(0, inner).Data);
            Assert.Equal(CosName.Create("F3"), AssertDataToken<NameToken, CosName>(1, inner).Data);

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

            Assert.Equal(CosName.Create("Bounds"), AssertDataToken<NameToken, CosName>(0, array).Data);

            var firstInner = AssertArrayToken(array.Data[1]);

            var firstFirstInner = AssertArrayToken(firstInner.Data[0]);

            Assert.Equal(19m, AssertDataToken<NumericToken, decimal>(0, firstFirstInner).Data);
            Assert.Equal(-69m, AssertDataToken<NumericToken, decimal>(1, firstFirstInner).Data);
            
            var secondFirstInner = AssertArrayToken(firstInner.Data[1]);

            Assert.Equal(7m, AssertDataToken<NumericToken, decimal>(0, secondFirstInner).Data);
            Assert.Equal(64.625m, AssertDataToken<NumericToken, decimal>(1, secondFirstInner).Data);

            Assert.Equal("More", AssertDataToken<StringToken, string>(2, array).Data);

            var secondInner = AssertArrayToken(array.Data[3]);

            var firstSecondInner = AssertArrayToken(secondInner.Data[0]);

            var firstFirstSecondInner = AssertArrayToken(firstSecondInner.Data[0]);

            Assert.Equal(15m, AssertDataToken<NumericToken, decimal>(0, firstFirstSecondInner).Data);
        }

        [Fact]
        public void SpecificationExampleArray()
        {
            const string s = "[549 3.14 false (Ralph) /SomeName]";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var array = AssertArrayToken(token);

            Assert.Equal(549m, AssertDataToken<NumericToken, decimal>(0, array).Data);
            Assert.Equal(3.14m, AssertDataToken<NumericToken, decimal>(1, array).Data);
            Assert.False(AssertDataToken<BooleanToken, bool>(2, array).Data);
            Assert.Equal("Ralph", AssertDataToken<StringToken, string>(3, array).Data);
            Assert.Equal(CosName.Create("SomeName"), AssertDataToken<NameToken, CosName>(4, array).Data);
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
