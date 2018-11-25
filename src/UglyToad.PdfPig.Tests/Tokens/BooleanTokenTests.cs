namespace UglyToad.PdfPig.Tests.Tokens
{
    using PdfPig.Tokens;
    using Xunit;

    public class BooleanTokenTests
    {
        [Fact]
        public void BooleanTokensObjectEquals()
        {
            var one = BooleanToken.True;
            var two = (object) BooleanToken.True;

            Assert.True(one.Equals(two));
        }

        [Fact]
        public void BooleanTokensObjectNotEqual()
        {
            var one = BooleanToken.False;
            var two = (object)BooleanToken.True;

            Assert.False(one.Equals(two));
        }

        [Fact]
        public void BooleanTokensHashCodeMatch()
        {
            Assert.Equal(BooleanToken.True.GetHashCode(), BooleanToken.True.GetHashCode());
        }

        [Fact]
        public void BooleanTokensHashCodeNotMatch()
        {
            Assert.NotEqual(BooleanToken.True.GetHashCode(), BooleanToken.False.GetHashCode());
        }

        [Fact]
        public void BooleanTokensStringRepresentationCorrect()
        {
            Assert.Equal("True", BooleanToken.True.ToString());
            Assert.Equal("False", BooleanToken.False.ToString());
        }
    }
}
