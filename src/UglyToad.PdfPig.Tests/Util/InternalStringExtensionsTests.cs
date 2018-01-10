namespace UglyToad.PdfPig.Tests.Util
{
    using System;
    using PdfPig.Util;
    using Xunit;

    public class InternalStringExtensionsTests
    {
        [Theory]
        [InlineData("a", "a", 0, true)]
        [InlineData("a", "a", 1, false)]
        [InlineData("", "abc", 0, false)]
        [InlineData("abc", "abc", 1, false)]
        [InlineData("abc", "abc", 10, false)]
        [InlineData("pineapple", "apple", 0, false)]
        [InlineData("pineapple", "apple", 3, false)]
        [InlineData("pineapple", "apple", 4, true)]
        public void StartsWithOffset(string input, string start, int offset, bool expected)
        {
            var result = input.StartsWithOffset(start, offset);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void StartsWithOffset_NegativeOffset_Throws()
        {
            Action action = () => "any".StartsWithOffset("x", -1);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }
    }
}
