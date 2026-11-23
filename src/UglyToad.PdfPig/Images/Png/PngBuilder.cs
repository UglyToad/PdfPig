namespace UglyToad.PdfPig.Images.Png
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.IO.Compression;
    using UglyToad.PdfPig.Filters;

    /// <summary>
    /// Used to construct 8-bit RGB or RGBA PNG images. Rows are compressed as they are written, so the
    /// whole image is never held uncompressed. Call <see cref="Create"/> to make a new builder.
    /// </summary>
    internal sealed class PngBuilder : IDisposable
    {
        private const byte Deflate32KbWindow = 120;
        private const byte ChecksumBits = 1;

        // Rows go to the compressor in pieces of up to this many bytes, or one row at a time if longer.
        private const int PieceLength = 64 * 1024;

        private readonly bool hasAlphaChannel;
        private readonly int width;
        private readonly int height;
        private readonly int rowLength;
        private readonly byte[] piece;
        private readonly MemoryStream compressed;
        private readonly Adler32ChecksumStream compressor;
        private int rowsInPiece;
        private int rowsWritten;

        /// <summary>
        /// Create a builder for a PNG with the given width and size.
        /// </summary>
        public static PngBuilder Create(int width, int height, bool hasAlphaChannel)
        {
            return new PngBuilder(width, height, hasAlphaChannel);
        }

        private PngBuilder(int width, int height, bool hasAlphaChannel)
        {
            this.hasAlphaChannel = hasAlphaChannel;
            this.width = width;
            this.height = height;

            // Each row starts with its filter type, which stays 0 (none).
            rowLength = checked((width * (hasAlphaChannel ? 4 : 3)) + 1);
            piece = new byte[Math.Max(1, Math.Min(height, PieceLength / rowLength)) * rowLength];

            compressed = new MemoryStream();
            compressed.WriteByte(Deflate32KbWindow);
            compressed.WriteByte(ChecksumBits);
            compressor = new Adler32ChecksumStream(new DeflateStream(compressed, CompressionLevel.Fastest, true));
        }

        /// <summary>
        /// The pixels of the next row, to be filled before calling <see cref="WriteRow"/>.
        /// </summary>
        public Span<byte> NextRow()
        {
            return piece.AsSpan((rowsInPiece * rowLength) + 1, rowLength - 1);
        }

        /// <summary>
        /// Add the row returned by <see cref="NextRow"/> to the image.
        /// </summary>
        public void WriteRow()
        {
            rowsWritten++;
            rowsInPiece++;

            if ((rowsInPiece + 1) * rowLength > piece.Length)
            {
                WritePiece();
            }
        }

        private void WritePiece()
        {
            compressor.Write(piece, 0, rowsInPiece * rowLength);
            rowsInPiece = 0;
        }

        /// <summary>
        /// Get the bytes of the PNG file once all rows are written.
        /// </summary>
        public byte[] Save()
        {
            if (rowsWritten != height)
            {
                throw new InvalidOperationException($"Wrote {rowsWritten} rows of an image with {height}.");
            }

            WritePiece();
            compressor.Close();

            Span<byte> checksum = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(checksum, compressor.Checksum);
            compressed.Write(checksum);

            var imageData = compressed.GetBuffer().AsSpan(0, (int)compressed.Length);

            Span<byte> header = stackalloc byte[13];
            BinaryPrimitives.WriteInt32BigEndian(header, width);
            BinaryPrimitives.WriteInt32BigEndian(header.Slice(4), height);
            header[8] = 8;
            header[9] = (byte)(hasAlphaChannel ? ColorType.ColorUsed | ColorType.AlphaChannelUsed : ColorType.ColorUsed);
            header[10] = (byte)CompressionMethod.DeflateWithSlidingWindow;
            header[11] = (byte)FilterMethod.AdaptiveFiltering;
            header[12] = (byte)InterlaceMethod.None;

            var signature = HeaderValidationResult.ExpectedHeader;
            // Three chunks, each with four bytes of length, type and CRC around its data.
            var result = new byte[signature.Length + (3 * 12) + header.Length + imageData.Length];
            signature.CopyTo(result);

            var offset = WriteChunk(result, signature.Length, ImageHeader.HeaderBytes, header);
            offset = WriteChunk(result, offset, "IDAT"u8, imageData);
            WriteChunk(result, offset, "IEND"u8, ReadOnlySpan<byte>.Empty);

            return result;
        }

        private static int WriteChunk(Span<byte> target, int offset, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
        {
            BinaryPrimitives.WriteInt32BigEndian(target.Slice(offset), data.Length);
            type.CopyTo(target.Slice(offset + 4));
            data.CopyTo(target.Slice(offset + 8));
            BinaryPrimitives.WriteUInt32BigEndian(target.Slice(offset + 8 + data.Length), Crc32.Calculate(type, data));
            return offset + 12 + data.Length;
        }

        public void Dispose()
        {
            compressor.Dispose();
            compressed.Dispose();
        }
    }
}