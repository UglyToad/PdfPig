namespace UglyToad.PdfPig.Tests.Filters.Jbig2
{
    using Xunit;
    using UglyToad.PdfPig.Filters.Jbig2;
    using UglyToad.PdfPig.Tests.Images;

    public class HalftoneRegionTest
    {
        [Fact]
        public void ParseHeaderTest()
        {
            var iis = new ImageInputStream(ImageHelpers.LoadFileBytes("sampledata.jb2"));
            // Seventh Segment (number 6)
            var sis = new SubInputStream(iis, 302, 87);
            var hr = new HalftoneRegion(sis);
            hr.Init(null, sis);

            Assert.True(hr.IsMMREncoded);
            Assert.Equal(0, hr.HTemplate);
            Assert.False(hr.HSkipEnabled);
            Assert.Equal(CombinationOperator.OR, hr.HCombinationOperator);
            Assert.Equal(0, hr.HDefaultPixel);
            Assert.Equal(8, hr.HGridWidth);
            Assert.Equal(9, hr.HGridHeight);
            Assert.Equal(0, hr.HGridX);
            Assert.Equal(0, hr.HGridY);
            Assert.Equal(1024, hr.HRegionX);
            Assert.Equal(0, hr.HRegionY);
        }
    }

}
