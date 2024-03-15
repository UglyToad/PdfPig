namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using PdfPig.Core;

    public class ReadHelperTests
    {
        [Fact]
        public void IsEndOfLine_LineFeed()
        {
            const char c = '\n';

            var result = ReadHelper.IsEndOfLine(c);

            Assert.True(result);
        }

        [Fact]
        public void IsEndOfLine_CarriageReturn()
        {
            const char c = '\r';

            var result = ReadHelper.IsEndOfLine(c);

            Assert.True(result);
        }

        [Theory]
        [InlineData('\0')]
        [InlineData('\f')]
        [InlineData('\\')]
        [InlineData('\b')]
        [InlineData('a')]
        [InlineData(' ')]
        public void IsEndOfLine_OtherCharacters(char c)
        {
            var result = ReadHelper.IsEndOfLine(c);

            Assert.False(result);
        }
    }
}
