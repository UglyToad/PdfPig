// ReSharper disable ObjectCreationAsStatement
namespace UglyToad.PdfPig.Tests.Fonts.Cmap
{
    using PdfFonts.Cmap;

    public class CidRangeTests
    {
        [Fact]
        public void EndCannotBeLowerThanStart()
        {
            Action action = () => new CidRange(0, -56, 1);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void ContainsFalseForLowerNumber()
        {
            var range = new CidRange(0, 69, 0);

            var result = range.Contains(-12);

            Assert.False(result);
        }

        [Fact]
        public void ContainsFalseForHigherNumber()
        {
            var range = new CidRange(0, 69, 0);

            var result = range.Contains(100);

            Assert.False(result);
        }

        [Fact]
        public void ContainsTrueForNumberInRange()
        {
            var range = new CidRange(0, 69, 0);

            var result = range.Contains(52);

            Assert.True(result);
        }

        [Fact]
        public void TryMapFalseForNumberLowerThanRange()
        {
            var range = new CidRange(0, 69, 0);

            var result = range.TryMap(-12, out var _);

            Assert.False(result);
        }

        [Fact]
        public void TryMapFalseForNumberHigherThanRange()
        {
            var range = new CidRange(0, 69, 0);

            var result = range.TryMap(250, out var _);

            Assert.False(result);
        }

        [Fact]
        public void TryMapMapsCorrectlyForNumberInRange()
        {
            var range = new CidRange(0, 69, 0);

            var result = range.TryMap(52, out var cid);

            Assert.True(result);

            Assert.Equal(52, cid);
        }

        [Fact]
        public void TryMapMapsCorrectlyForNumberInRangeWithCidOffset()
        {
            var range = new CidRange(0, 69, 9);

            var result = range.TryMap(52, out var cid);

            Assert.True(result);

            Assert.Equal(61, cid);
        }
    }
}
