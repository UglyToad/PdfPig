namespace UglyToad.PdfPig.Tests.Util
{
    using PdfPig.Core;
    using Xunit;

    public class OtherEncodingsTests
    {
        [Fact]
        public void BytesNullReturnsNullString()
        {
            var result = OtherEncodings.BytesAsLatin1String(null);

            Assert.Null(result);
        }

        [Fact]
        public void BytesEmptyReturnsEmptyString()
        {
            var result = OtherEncodings.BytesAsLatin1String(new byte[0]);

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
