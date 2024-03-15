namespace UglyToad.PdfPig.Tests.IO
{
    using PdfPig.Core;
 
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

        [Fact]
        public void ReadFromBeginningIsCorrect()
        {
            var bytes = StringToBytes("endstream and then <</go[]>>");

            var buffer = new byte["endstream".Length];

            var result = bytes.Read(buffer);

            Assert.Equal(buffer.Length, result);
            Assert.Equal("endstream", OtherEncodings.BytesAsLatin1String(buffer));

            Assert.Equal((byte)'m', bytes.CurrentByte);
            Assert.True(bytes.MoveNext());
            Assert.True(bytes.MoveNext());
            Assert.Equal((byte)'a', bytes.CurrentByte);
        }

        [Fact]
        public void ReadMatchesMoveBehaviour()
        {
            var bytesRead = StringToBytes("cows in the south");
            var bytesMove = StringToBytes("cows in the north");

            const int readLength = 3;

            var buffer = new byte[readLength];

            var readResult = bytesRead.Read(buffer);

            for (var i = 0; i < readLength; i++)
            {
                bytesMove.MoveNext();
            }

            Assert.Equal(readLength, readResult);

            Assert.Equal(bytesRead.CurrentOffset, bytesMove.CurrentOffset);
            Assert.Equal(bytesRead.CurrentByte, bytesMove.CurrentByte);
            Assert.Equal(bytesRead.MoveNext(), bytesMove.MoveNext());
            Assert.Equal(bytesRead.CurrentOffset, bytesMove.CurrentOffset);
            Assert.Equal(bytesRead.CurrentByte, bytesMove.CurrentByte);
        }

        [Fact]
        public void ReadFromMiddleIsCorrect()
        {
            var bytes = StringToBytes("aa stream <<>>");

            Assert.True(bytes.MoveNext());
            Assert.True(bytes.MoveNext());
            Assert.True(bytes.MoveNext());

            Assert.Equal((byte)' ', bytes.CurrentByte);

            var buffer = new byte["stream".Length];

            var result = bytes.Read(buffer);

            Assert.Equal(buffer.Length, result);
            Assert.Equal("stream", OtherEncodings.BytesAsLatin1String(buffer));

            Assert.Equal((byte)'m', bytes.CurrentByte);
            Assert.True(bytes.MoveNext());
            Assert.True(bytes.MoveNext());
            Assert.Equal((byte)'<', bytes.CurrentByte);
        }

        [Fact]
        public void ReadPastEndIsCorrect()
        {
            var bytes = StringToBytes("stream");

            Assert.True(bytes.MoveNext());
            Assert.True(bytes.MoveNext());

            var buffer = new byte["stream".Length];

            var result = bytes.Read(buffer);

            Assert.Equal(buffer.Length - 2, result);
            Assert.Equal("ream", OtherEncodings.BytesAsLatin1String(buffer.Take(buffer.Length - 2).ToArray()));

            Assert.Equal((byte)'m', bytes.CurrentByte);
            Assert.True(bytes.IsAtEnd());
            Assert.False(bytes.MoveNext());
        }

        [Fact]
        public void ReadFromStreamBeginningIsCorrect()
        {
            var stream = StringToStream("endstream and then <</go[]>>");

            var buffer = new byte["endstream".Length];

            var result = stream.Read(buffer);

            Assert.Equal(buffer.Length, result);
            Assert.Equal("endstream", OtherEncodings.BytesAsLatin1String(buffer));

            Assert.Equal((byte)'m', stream.CurrentByte);
            Assert.True(stream.MoveNext());
            Assert.True(stream.MoveNext());
            Assert.Equal((byte)'a', stream.CurrentByte);
        }

        [Fact]
        public void ReadFromStreamMiddleIsCorrect()
        {
            var stream = StringToStream("aa stream <<>>");

            Assert.True(stream.MoveNext());
            Assert.True(stream.MoveNext());
            Assert.True(stream.MoveNext());

            Assert.Equal((byte)' ', stream.CurrentByte);

            var buffer = new byte["stream".Length];

            var result = stream.Read(buffer);

            Assert.Equal(buffer.Length, result);
            Assert.Equal("stream", OtherEncodings.BytesAsLatin1String(buffer));

            Assert.Equal((byte)'m', stream.CurrentByte);
            Assert.True(stream.MoveNext());
            Assert.True(stream.MoveNext());
            Assert.Equal((byte)'<', stream.CurrentByte);
        }

        [Fact]
        public void ReadPastStreamEndIsCorrect()
        {
            var stream = StringToStream("stream");

            Assert.True(stream.MoveNext());
            Assert.True(stream.MoveNext());

            var buffer = new byte["stream".Length];

            var result = stream.Read(buffer);

            Assert.Equal(buffer.Length - 2, result);
            Assert.Equal("ream", OtherEncodings.BytesAsLatin1String(buffer.Take(buffer.Length - 2).ToArray()));

            Assert.Equal((byte)'m', stream.CurrentByte);
            Assert.True(stream.IsAtEnd());
            Assert.False(stream.MoveNext());
        }

        private static ByteArrayInputBytes StringToBytes(string str) => new ByteArrayInputBytes(OtherEncodings.StringAsLatin1Bytes(str));
        private static StreamInputBytes StringToStream(string str) => new StreamInputBytes(new MemoryStream(OtherEncodings.StringAsLatin1Bytes(str)));
    }
}
