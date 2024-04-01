namespace UglyToad.PdfPig.Tests.Fonts.TrueType
{
    using PdfPig.Core;
    using PdfPig.Fonts.TrueType;

    public class TrueTypeDataBytesTests
    {
        [Fact]
        public void ReadUnsignedInt()
        {
            var input = new MemoryInputBytes(new byte[]
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
