namespace UglyToad.PdfPig.Tests.Filters.Jbig2
{
    using Xunit;
    using UglyToad.PdfPig.Filters.Jbig2;
    using UglyToad.PdfPig.Tests.Images;

    public class ArithmeticIntegerDecoderTest
    {
        [Fact]
        public void DecodeTest()
        {
            var iis = new ImageInputStream(ImageHelpers.LoadFileBytes("arith-encoded-testsequence.bin"));
            var ad = new ArithmeticDecoder(iis);
            var aid = new ArithmeticIntegerDecoder(ad);

            long result = aid.Decode(null);

            Assert.Equal(1, result);
        }
    }
}
