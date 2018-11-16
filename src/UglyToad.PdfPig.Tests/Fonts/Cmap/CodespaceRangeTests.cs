namespace UglyToad.PdfPig.Tests.Fonts.Cmap
{
    using System;
    using System.Linq;
    using PdfPig.Fonts.Cmap;
    using PdfPig.Tokens;
    using Xunit;

    public class CodespaceRangeTests
    {
        [Theory]
        [InlineData("00", "80", 0, 128, 1)]
        [InlineData("8140", "9ffc", 33088, 40956, 2)]
        public void CreatesCorrectly(string startHex, string endHex, int startInt, int endInt, int codeLength)
        {
            var range = new CodespaceRange(GetHexBytes(startHex.ToCharArray()),
                GetHexBytes(endHex.ToCharArray()));

            Assert.Equal(startInt, range.StartInt);
            Assert.Equal(endInt, range.EndInt);

            Assert.Equal(codeLength, range.CodeLength);
        }

        [Fact]
        public void MatchesCodeIsNullThrows()
        {
            var start = GetHexBytes('0', 'A');
            var end = GetHexBytes('8', '0' );

            var codespace = new CodespaceRange(start, end);

            Action action = () => codespace.Matches(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void IsFullMatchCodeIsNullThrows()
        {
            var start = GetHexBytes('0', 'A');
            var end = GetHexBytes('8', '0');

            var codespace = new CodespaceRange(start, end);

            Action action = () => codespace.IsFullMatch(null, 2);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void MatchesCodeWrongLengthFalse()
        {
            var start = GetHexBytes('0', 'A');
            var end = GetHexBytes('8', '0');

            var codespace = new CodespaceRange(start, end);

            var matches = codespace.Matches(GetHexBytes('6', '9', '0', '1'));

            Assert.False(matches);
        }

        [Fact]
        public void MatchesCodeLowerThanStartFalse()
        {
            var start = GetHexBytes('0', 'A');
            var end = GetHexBytes('8', '0');

            var codespace = new CodespaceRange(start, end);

            var matches = codespace.Matches(GetHexBytes('0', '1'));

            Assert.False(matches);
        }

        [Fact]
        public void MatchesCodeHigherThanEndFalse()
        {
            var start = GetHexBytes('0', 'A');
            var end = GetHexBytes('8', '0');

            var codespace = new CodespaceRange(start, end);

            var matches = codespace.Matches(GetHexBytes('9', '6'));

            Assert.False(matches);
        }

        [Fact]
        public void MatchesCodeInRangeTrue()
        {
            var start = GetHexBytes('0', 'A');
            var end = GetHexBytes('8', '0');

            var codespace = new CodespaceRange(start, end);

            var matches = codespace.Matches(GetHexBytes('5', 'A'));

            Assert.True(matches);
        }

        private static byte[] GetHexBytes(params char[] characters)
        {
            var token = new HexToken(characters);

            return token.Bytes.ToArray();
        }
    }
}
