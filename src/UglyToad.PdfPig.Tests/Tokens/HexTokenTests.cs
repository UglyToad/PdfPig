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
    }
}
