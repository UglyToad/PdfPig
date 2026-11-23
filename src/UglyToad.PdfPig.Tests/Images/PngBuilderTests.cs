namespace UglyToad.PdfPig.Tests.Images
{
    using System.Buffers.Binary;
    using System.IO.Compression;
    using System.Text;
    using UglyToad.PdfPig.Images.Png;

    public class PngBuilderTests
    {
        [Theory]
        [InlineData(1, 1, false)]
        [InlineData(3, 3, true)]
        [InlineData(200, 500, false)] // Several pieces of rows, the last one shorter.
        [InlineData(200, 500, true)]
        [InlineData(21845, 3, false)] // Each row exactly fills a piece.
        [InlineData(21846, 2, false)] // Each row is longer than a piece.
        [InlineData(16384, 2, true)]
        public void RowsReadBackFromThePng(int width, int height, bool hasAlphaChannel)
        {
            var bytesPerPixel = hasAlphaChannel ? 4 : 3;
            var rowLength = (width * bytesPerPixel) + 1;

            // What the image data must inflate to: each row has filter type 0 (none) before its samples.
            var expectedRows = new byte[height * rowLength];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width * bytesPerPixel; x++)
                {
                    expectedRows[(y * rowLength) + 1 + x] = Sample(x, y);
                }
            }

            byte[] bytes;
            using (var builder = PngBuilder.Create(width, height, hasAlphaChannel))
            {
                for (var y = 0; y < height; y++)
                {
                    var row = builder.NextRow();
                    Assert.Equal(width * bytesPerPixel, row.Length);
                    expectedRows.AsSpan((y * rowLength) + 1, row.Length).CopyTo(row);
                    builder.WriteRow();
                }

                bytes = builder.Save();
            }

            var chunks = ReadChunks(bytes);
            Assert.Equal(new[] { "IHDR", "IDAT", "IEND" }, chunks.Select(x => x.Type));

            var imageData = chunks[1].Data;
            Assert.Equal(0x78, imageData[0]);
            Assert.Equal(0x01, imageData[1]);

            using (var deflate = new DeflateStream(new MemoryStream(imageData, 2, imageData.Length - 6), CompressionMode.Decompress))
            using (var rows = new MemoryStream())
            {
                deflate.CopyTo(rows);
                Assert.Equal(expectedRows, rows.ToArray());
            }

            Assert.Equal(Adler32(expectedRows), BinaryPrimitives.ReadUInt32BigEndian(imageData.AsSpan(imageData.Length - 4)));

            // The chunk CRCs are checked when the PNG is opened.
            var png = Png.Open(bytes);
            Assert.Equal(width, png.Width);
            Assert.Equal(height, png.Height);
            Assert.Equal(hasAlphaChannel, png.HasAlphaChannel);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = x * bytesPerPixel;
                    var alpha = hasAlphaChannel ? Sample(i + 3, y) : byte.MaxValue;
                    Assert.Equal(new Pixel(Sample(i, y), Sample(i + 1, y), Sample(i + 2, y), alpha, false), png.GetPixel(x, y));
                }
            }
        }

        [Fact]
        public void SaveThrowsWhenRowsAreMissing()
        {
            using var builder = PngBuilder.Create(2, 2, false);
            builder.NextRow().Fill(1);
            builder.WriteRow();

            Assert.Throws<InvalidOperationException>(() => builder.Save());
        }

        private static byte Sample(int x, int y) => (byte)((((uint)x * 2654435761u) ^ ((uint)y * 2246822519u)) >> 24);

        private static List<(string Type, byte[] Data)> ReadChunks(byte[] png)
        {
            Assert.Equal(HeaderValidationResult.ExpectedHeader.ToArray(), png.Take(8));

            var chunks = new List<(string, byte[])>();
            var offset = 8;
            while (offset < png.Length)
            {
                var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset));
                chunks.Add((Encoding.ASCII.GetString(png, offset + 4, 4), png.AsSpan(offset + 8, length).ToArray()));
                offset += 12 + length;
            }

            Assert.Equal(png.Length, offset);

            return chunks;
        }

        private static uint Adler32(ReadOnlySpan<byte> data)
        {
            uint a = 1;
            uint b = 0;
            foreach (var value in data)
            {
                a = (a + value) % 65521;
                b = (b + a) % 65521;
            }

            return (b << 16) | a;
        }
    }
}
