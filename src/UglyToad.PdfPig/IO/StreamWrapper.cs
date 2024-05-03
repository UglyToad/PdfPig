namespace UglyToad.PdfPig.IO
{
    using System.IO;

    internal class StreamWrapper : Stream
    {
        protected readonly Stream Stream;

        public StreamWrapper(Stream stream)
        {
            Stream = stream;
        }

        public override void Flush()
        {
            Stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Stream.SetLength(value);
        }

#if NET
        public override int Read(Span<byte> buffer)
        {
            return Stream.Read(buffer);
        }
#endif

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }

#if NET
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Stream.Write(buffer);
        }
#endif

        public override bool CanRead => Stream.CanRead;

        public override bool CanSeek => Stream.CanSeek;

        public override bool CanWrite => Stream.CanWrite;

        public override long Length => Stream.Length;

        public override long Position
        {
            get => Stream.Position;
            set => Stream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Stream?.Dispose();
        }
    }
}
