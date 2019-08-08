namespace UglyToad.PdfPig.Tests.IO
{
    using System.IO;
    using PdfPig.IO;
    using PdfPig.Util;
    using Xunit;

    public class InputBytesTests
    {
        private const string TestData = @"123456789";

        [Fact]
        public void ArrayAndStreamBehaveTheSame()
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(TestData);

            var array = new ByteArrayInputBytes(bytes);

            using (var memoryStream = new MemoryStream(bytes))
            {
                var stream = new StreamInputBytes(memoryStream);

                Assert.Equal(bytes.Length, array.Length);
                Assert.Equal(bytes.Length, stream.Length);

                Assert.Equal(0, array.CurrentOffset);
                Assert.Equal(0, stream.CurrentOffset);

                array.Seek(5);
                stream.Seek(5);

                Assert.Equal(array.CurrentOffset, stream.CurrentOffset);

                Assert.Equal((byte)'5', array.CurrentByte);
                Assert.Equal(array.CurrentByte, stream.CurrentByte);

                Assert.Equal(array.Peek(), stream.Peek());

                array.Seek(0);
                stream.Seek(0);

                Assert.Equal(0, array.CurrentByte);
                Assert.Equal(array.CurrentByte, stream.CurrentByte);

                array.Seek(7);
                stream.Seek(7);

                var arrayString = string.Empty;
                var streamString = string.Empty;

                while (array.MoveNext())
                {
                    arrayString += (char) array.CurrentByte;
                }

                while (stream.MoveNext())
                {
                    streamString += (char) stream.CurrentByte;
                }

                Assert.Equal("89", streamString);

                Assert.Equal(arrayString, streamString);

                Assert.True(stream.IsAtEnd());
                Assert.True(array.IsAtEnd());

                stream.Seek(0);
                array.Seek(0);

                Assert.False(stream.IsAtEnd());
                Assert.False(array.IsAtEnd());
            }
        }
    }
}
