namespace UglyToad.PdfPig.Tests.Util
{
    using PdfPig.Core;

    public class OtherEncodingsTests
    {
        [Fact]
        public void BytesEmptyReturnsEmptyString()
        {
            var result = OtherEncodings.BytesAsLatin1String([]);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void StringNullReturnsNullBytes()
        {
            var result = OtherEncodings.StringAsLatin1Bytes(null);

            Assert.Null(result);
        }
    }
}
