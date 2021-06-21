namespace UglyToad.PdfPig.Tests.Filters.Jbig2
{
    using UglyToad.PdfPig.Filters.Jbig2;
    using UglyToad.PdfPig.Tests.Images;
    using Xunit;

    public class RegionSegmentInformationTest
    {
        [Fact]
        public void ParseHeaderTest()
        {
            var iis = new ImageInputStream(ImageHelpers.LoadFileBytes("sampledata.jb2"));
            var sis = new SubInputStream(iis, 130, 49);
            var rsi = new RegionSegmentInformation(sis);
            rsi.ParseHeader();

            Assert.Equal(37, rsi.BitmapWidth);
            Assert.Equal(8, rsi.BitmapHeight);
            Assert.Equal(4, rsi.X);
            Assert.Equal(1, rsi.Y);
            Assert.Equal(CombinationOperator.OR, rsi.CombinationOperator);
        }
    }
}
