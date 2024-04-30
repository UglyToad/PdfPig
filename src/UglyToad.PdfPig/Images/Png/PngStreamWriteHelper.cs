namespace UglyToad.PdfPig.Images.Png
{
    using System;
    using System.Buffers.Binary;
    using System.IO;

    internal sealed class PngStreamWriteHelper : Stream
    {
        private readonly Stream inner;
        private readonly Crc32 crc = new();

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => inner.CanSeek;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public PngStreamWriteHelper(Stream inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public override void Flush() => inner.Flush();

        public void WriteChunkHeader(ReadOnlySpan<byte> header)
        {
            crc.Reset();
            Write(header);
        }

        public void WriteChunkLength(int length)
        {
            StreamHelper.WriteBigEndianInt32(inner, length);
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            crc.Append(buffer.AsSpan(offset, count));
            inner.Write(buffer, offset, count);
        }

#if NET
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            crc.Append(buffer);
            inner.Write(buffer);
        }
#else
        public void Write(ReadOnlySpan<byte> buffer)
        {
            crc.Append(buffer);
            inner.Write(buffer);
        }
#endif

        public void WriteCrc()
        {
            Span<byte> buffer = stackalloc byte[4];

            var result = crc.GetCurrentHashAsUInt32();

            BinaryPrimitives.WriteUInt32BigEndian(buffer, result);

            inner.Write(buffer);
        }
    }
}