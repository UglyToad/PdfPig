namespace UglyToad.PdfPig.Tests.Filters.Jbig2
{
    using Xunit;
    using UglyToad.PdfPig.Filters.Jbig2;
    using UglyToad.PdfPig.Tests.Images;

    public class GenericRegionTest
    {
        [Fact]
        public void ParseHeaderTest()
        {
            var iis = new ImageInputStream(ImageHelpers.LoadFileBytes("sampledata.jb2"));

            // Twelfth Segment (number 11)
            var sis = new SubInputStream(iis, 523, 35);
            var gr = new GenericRegion();
            gr.Init(null, sis);

            Assert.Equal(54, gr.RegionInfo.BitmapWidth);
            Assert.Equal(44, gr.RegionInfo.BitmapHeight);
            Assert.Equal(4, gr.RegionInfo.X);
            Assert.Equal(11, gr.RegionInfo.Y);
            Assert.Equal(CombinationOperator.OR, gr.RegionInfo.CombinationOperator);
            Assert.False(gr.UseExtTemplates);
            Assert.False(gr.IsMMREncoded);
            Assert.Equal(0, gr.GbTemplate);
            Assert.True(gr.IsTPGDon);

            short[] gbAtX = gr.GbAtX;
            short[] gbAtY = gr.GbAtY;
            Assert.Equal(3, gbAtX[0]);
            Assert.Equal(-1, gbAtY[0]);
            Assert.Equal(-3, gbAtX[1]);
            Assert.Equal(-1, gbAtY[1]);
            Assert.Equal(2, gbAtX[2]);
            Assert.Equal(-2, gbAtY[2]);
            Assert.Equal(-2, gbAtX[3]);
            Assert.Equal(-2, gbAtY[3]);
        }
    }
}
