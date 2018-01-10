namespace UglyToad.PdfPig.Tests.Fonts.TrueType
{
    using IO;
    using PdfPig.Fonts.TrueType;
    using Xunit;

    public class TrueTypeDataBytesTests
    {
        [Fact]
        public void ReadUnsignedInt()
        {
            var input = new ByteArrayInputBytes(new byte[]
            {
                220,
                43,
                250,
                6
            });

            var data = new TrueTypeDataBytes(input);

            var result = data.ReadUnsignedInt();

            Assert.Equal(3693869574L, result);
        }
    }
}
