namespace UglyToad.PdfPig.Tests.Filters.Jbig2
{
    using Xunit;
    using UglyToad.PdfPig.Filters.Jbig2;
    using UglyToad.PdfPig.Tests.Images;

    public class PatternDictionaryTest
    {
        [Fact]
        public void ParseHeaderTest()
        {
            var iis = new ImageInputStream(ImageHelpers.LoadFileBytes("sampledata.jb2"));
            // Sixth Segment (number 5)
            var sis = new SubInputStream(iis, 245, 45);
            var pd = new PatternDictionary();
            pd.Init(null, sis);

            Assert.True(pd.IsMMREncoded);
            Assert.Equal(0, pd.HdTemplate);
            Assert.Equal(4, pd.HdpWidth);
            Assert.Equal(4, pd.HdpHeight);
            Assert.Equal(15, pd.GrayMax);
        }
    }
}
