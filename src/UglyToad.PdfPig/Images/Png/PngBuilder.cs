namespace UglyToad.PdfPig.Images.Png
{
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    /// <summary>
    /// Used to construct PNG images. Call <see cref="Create"/> to make a new builder.
    /// </summary>
    internal class PngBuilder
    {
        private const byte Deflate32KbWindow = 120;
        private const byte ChecksumBits = 1;

        private readonly byte[] rawData;
        private readonly bool hasAlphaChannel;
        private readonly int width;
        private readonly int height;
        private readonly int bytesPerPixel;

        /// <summary>
        /// Create a builder for a PNG with the given width and size.
        /// </summary>
        public static PngBuilder Create(int width, int height, bool hasAlphaChannel)
        {
            var bpp = hasAlphaChannel ? 4 : 3;

            var length = (height * width * bpp) + height;

            return new PngBuilder(new byte[length], hasAlphaChannel, width, height, bpp);
        }

        private PngBuilder(byte[] rawData, bool hasAlphaChannel, int width, int height, int bytesPerPixel)
        {
            this.rawData = rawData;
            this.hasAlphaChannel = hasAlphaChannel;
            this.width = width;
            this.height = height;
            this.bytesPerPixel = bytesPerPixel;
        }

        /// <summary>
        /// Sets the RGB pixel value for the given column (x) and row (y).
        /// </summary>
        public PngBuilder SetPixel(byte r, byte g, byte b, int x, int y) => SetPixel(new Pixel(r, g, b), x, y);

        /// <summary>
        /// Set the pixel value for the given column (x) and row (y).
        /// </summary>
        public PngBuilder SetPixel(Pixel pixel, int x, int y)
        {
            var start = (y * ((width * bytesPerPixel) + 1)) + 1 + (x * bytesPerPixel);

            rawData[start++] = pixel.R;
            rawData[start++] = pixel.G;
            rawData[start++] = pixel.B;

            if (hasAlphaChannel)
            {
                rawData[start] = pixel.A;
            }

            return this;
        }
        
        /// <summary>
        /// Get the bytes of the PNG file for this builder.
        /// </summary>
        public byte[] Save()
        {
            using (var memoryStream = new MemoryStream())
            {
                Save(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Write the PNG file bytes to the provided stream.
        /// </summary>
        public void Save(Stream outputStream)
        {
            outputStream.Write(HeaderValidationResult.ExpectedHeader);

            var stream = new PngStreamWriteHelper(outputStream);

            stream.WriteChunkLength(13);
            stream.WriteChunkHeader(ImageHeader.HeaderBytes);

            StreamHelper.WriteBigEndianInt32(stream, width);
            StreamHelper.WriteBigEndianInt32(stream, height);
            stream.WriteByte(8);

            var colorType = ColorType.ColorUsed;
            if (hasAlphaChannel)
            {
                colorType |= ColorType.AlphaChannelUsed;
            }

            stream.WriteByte((byte)colorType);
            stream.WriteByte((byte)CompressionMethod.DeflateWithSlidingWindow);
            stream.WriteByte((byte)FilterMethod.AdaptiveFiltering);
            stream.WriteByte((byte)InterlaceMethod.None);

            stream.WriteCrc();

            var imageData = Compress(rawData);
            stream.WriteChunkLength(imageData.Length);
            stream.WriteChunkHeader("IDAT"u8);
            stream.Write(imageData);
            stream.WriteCrc();

            stream.WriteChunkLength(0);
            stream.WriteChunkHeader("IEND"u8);
            stream.WriteCrc();
        }

        private static byte[] Compress(byte[] data)
        {
            const int headerLength = 2;
            const int checksumLength = 4;
            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, CompressionLevel.Fastest, true))
            {
                compressor.Write(data, 0, data.Length);
                compressor.Close();

                compressStream.Seek(0, SeekOrigin.Begin);

                var result = new byte[headerLength + compressStream.Length + checksumLength];

                // Write the ZLib header.
                result[0] = Deflate32KbWindow;
                result[1] = ChecksumBits;

                // Write the compressed data.
                int streamValue;
                var i = 0;
                while ((streamValue = compressStream.ReadByte()) != -1)
                {
                    result[headerLength + i] = (byte) streamValue;
                    i++;
                }

                // Write Checksum of raw data.
                var checksum = Adler32Checksum.Calculate(data);

                var offset = headerLength + compressStream.Length;

                result[offset++] = (byte)(checksum >> 24);
                result[offset++] = (byte)(checksum >> 16);
                result[offset++] = (byte)(checksum >> 8);
                result[offset] = (byte)(checksum >> 0);

                return result;
            }
        }
    }
}