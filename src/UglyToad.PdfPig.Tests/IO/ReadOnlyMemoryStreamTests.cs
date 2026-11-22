namespace UglyToad.PdfPig.Tests.IO
{
    using PdfPig.IO;

    public class ReadOnlyMemoryStreamTests
    {
        [Fact]
        public void ReadsTheMemoryIntoThePartOfTheBufferAsked()
        {
            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            using (var stream = new ReadOnlyMemoryStream(bytes.AsMemory(1, 7)))
            {
                var buffer = new byte[8];

                Assert.Equal(5, stream.Read(buffer, 2, 5));
                Assert.Equal(new byte[] { 0, 0, 1, 2, 3, 4, 5, 0 }, buffer);

                Assert.Equal(2, stream.Read(buffer, 0, 8));
                Assert.Equal(new byte[] { 6, 7, 1, 2, 3, 4, 5, 0 }, buffer);

                Assert.Equal(0, stream.Read(buffer, 0, 8));
                Assert.Equal(7, stream.Position);
            }
        }
    }
}
