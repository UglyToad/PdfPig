namespace UglyToad.PdfPig.Tests.Tokens
{
    using PdfPig.Tokens;

    public class ArrayTokenTests
    {
        [Fact]
        public void SetsData()
        {
            var token = new ArrayToken(new[]
            {
                OperatorToken.StartStream,
                OperatorToken.EndStream
            });

            Assert.Equal(2, token.Data.Count);

            Assert.Equal(OperatorToken.StartStream, token.Data[0]);
            Assert.Equal(OperatorToken.EndStream, token.Data[1]);
        }

        [Fact]
        public void SetsDataEmpty()
        {
            var token = new ArrayToken(new IToken[0]);

            Assert.Empty(token.Data);
        }

        [Fact]
        public void SetsDataNullThrows()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action action = () => new ArrayToken(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void ToStringCorrect()
        {
            var token = new ArrayToken(new IToken[]
            {
                new StringToken("hedgehog"),
                new NumericToken(7),
                OperatorToken.StartObject
            });

            Assert.Equal("[ (hedgehog), 7, obj ]", token.ToString());
        }

        [Fact]
        public void EqualsAndGetHashCode()
        {
            var token1 = new ArrayToken(new IToken[] { new NumericToken(1), new NumericToken(2) });
            var token2 = new ArrayToken(new IToken[] { new NumericToken(1), new NumericToken(2) });
            var token3 = new ArrayToken(new IToken[] { new NumericToken(1), new NumericToken(3) });

            Assert.Equal(token1, token2);
            Assert.Equal(token1.GetHashCode(), token2.GetHashCode());
            Assert.NotEqual(token1, token3);
            Assert.False(token1.Equals(null));
            Assert.False(token1.Equals(new object()));
        }

        [Fact]
        public void EqualsIsOrderSensitive()
        {
            var token = new ArrayToken(new IToken[] { new NumericToken(1), new NumericToken(2) });
            var reordered = new ArrayToken(new IToken[] { new NumericToken(2), new NumericToken(1) });

            Assert.NotEqual(token, reordered);
            Assert.NotEqual(reordered, token);
        }

        [Fact]
        public void EqualsIsLengthSensitive()
        {
            var token = new ArrayToken(new IToken[] { new NumericToken(1), new NumericToken(2) });
            var prefix = new ArrayToken(new IToken[] { new NumericToken(1) });
            var longer = new ArrayToken(new IToken[] { new NumericToken(1), new NumericToken(2), new NumericToken(3) });

            Assert.NotEqual(token, prefix);
            Assert.NotEqual(prefix, token);
            Assert.NotEqual(token, longer);
            Assert.NotEqual(longer, token);
        }

        [Fact]
        public void EqualsEmptyAndNestedArrays()
        {
            var empty1 = new ArrayToken(new IToken[0]);
            var empty2 = new ArrayToken(new IToken[0]);

            Assert.Equal(empty1, empty2);
            Assert.Equal(empty1.GetHashCode(), empty2.GetHashCode());

            var nested1 = new ArrayToken(new IToken[] { new ArrayToken(new IToken[] { new NumericToken(1) }), new StringToken("a") });
            var nested2 = new ArrayToken(new IToken[] { new ArrayToken(new IToken[] { new NumericToken(1) }), new StringToken("a") });
            var nested3 = new ArrayToken(new IToken[] { new ArrayToken(new IToken[] { new NumericToken(2) }), new StringToken("a") });

            Assert.Equal(nested1, nested2);
            Assert.Equal(nested1.GetHashCode(), nested2.GetHashCode());
            Assert.NotEqual(nested1, nested3);
        }
    }
}
