namespace UglyToad.PdfPig.Tests.Filters.Jbig2
{
    using Xunit;
    using UglyToad.PdfPig.Filters.Jbig2;
    using UglyToad.PdfPig.Tests.Images;

    public class MMRDecompressorTest
    {
        [Fact]
        public void MmrDecodingTest()
        {
            var expected = new byte[]
            {
                0, 0, 2, 34, 38, 102, 239, 255, 2, 102, 102,
                238, 238, 239, 255, 255, 0, 2, 102, 102, 127,
                255, 255, 255, 0, 0, 0, 4, 68, 102, 102, 127
            };
            
            var iis = new ImageInputStream(ImageHelpers.LoadFileBytes("sampledata.jb2"));
            // Sixth Segment (number 5)
            var sis = new SubInputStream(iis, 252, 38);
            var mmrd = new MMRDecompressor(16 * 4, 4, sis);
            Bitmap b = mmrd.Uncompress();
            byte[] actual = b.GetByteArray();

            Assert.Equal(expected, actual);
        }
    }
}
