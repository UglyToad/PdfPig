namespace UglyToad.PdfPig.Tests.Tokens
{
    using System;
    using PdfPig.Tokens;
    using Xunit;

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
    }
}
