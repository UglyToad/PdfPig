namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.IO;

    internal sealed class Adler32ChecksumStream : Stream
    {
        private readonly Stream underlyingStream;

        public Adler32ChecksumStream(Stream writeStream)
        {
            underlyingStream = writeStream ?? throw new ArgumentNullException(nameof(writeStream));
        }
        public override bool CanRead => underlyingStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => underlyingStream.CanWrite;

        public override long Length => underlyingStream.Length;

        public override long Position { get => underlyingStream.Position; set => throw new NotImplementedException(); }

        public override void Flush()
        {
            underlyingStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = underlyingStream.Read(buffer, offset, count);

            if (n > 0)
            {
                UpdateAdler(buffer.AsSpan(offset, n));
            }
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            underlyingStream.Write(buffer, offset, count);

            if (count > 0)
            {
                UpdateAdler(buffer.AsSpan(offset, count));
            }
        }

        public uint Checksum { get; private set; } = 1;

        private void UpdateAdler(Span<byte> span)
        {
            const uint MOD_ADLER = 65521;
            uint a = Checksum & 0xFFFF;
            uint b = (Checksum >> 16) & 0xFFFF;

            foreach (byte c in span)
            {
                a = (a + c) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }

            Checksum = (b << 16) | a;
        }

        public override void Close()
        {
            underlyingStream.Close();
        }
    }
}