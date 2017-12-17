namespace UglyToad.Pdf.Tests.Fonts.TrueType
{
    using IO;
    using Pdf.Fonts.TrueType;
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
