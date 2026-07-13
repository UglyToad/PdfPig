namespace UglyToad.PdfPig.Tests.Tokens
{
    using PdfPig.Tokens;

    public class HexTokenTests
    {
        [Theory]
        [InlineData("AE", "®")]
        [InlineData("61", "a")]
        [InlineData("0061", "a")]
        [InlineData("7465787420736f", "text so")]
        [InlineData("6170", "ap")]
        [InlineData("617", "ap")]
        public void MapsCorrectlyToString(string input, string expected)
        {
            var token = new HexToken(input.ToCharArray());

            Assert.Equal(expected, token.Data);
        }

        [Theory]
        [InlineData("0003", 3)]
        [InlineData("0011", 17)]
        [InlineData("0024", 36)]
        [InlineData("0037", 55)]
        [InlineData("0044", 68)]
        [InlineData("005B", 91)]
        public void MapsCorrectlyToInt(string input, int expected)
        {
            var token = new HexToken(input.ToCharArray());

            var value = HexToken.ConvertHexBytesToInt(token);

            Assert.Equal(expected, value);
        }

        [Fact]
        public void EqualsAndGetHashCode()
        {
            var token1 = new HexToken("AE".ToCharArray());
            var token2 = new HexToken("AE".ToCharArray());
            var token3 = new HexToken("61".ToCharArray());

            Assert.Equal(token1, token2);
            Assert.Equal(token1.GetHashCode(), token2.GetHashCode());
            Assert.NotEqual(token1, token3);
            Assert.False(token1.Equals(null));
            Assert.False(token1.Equals(new object()));
        }

        [Fact]
        public void EqualsIgnoresHexDigitCase()
        {
            var upper = new HexToken("AE".ToCharArray());
            var lower = new HexToken("ae".ToCharArray());

            Assert.Equal(upper, lower);
            Assert.Equal(upper.GetHashCode(), lower.GetHashCode());
        }

        [Fact]
        public void NotEqualToStringTokenWithSameText()
        {
            var hex = new HexToken("61".ToCharArray());
            var str = new StringToken("a");

            Assert.Equal("a", hex.Data);
            Assert.False(hex.Equals(str));
            Assert.False(str.Equals(hex));
        }
    }
}
