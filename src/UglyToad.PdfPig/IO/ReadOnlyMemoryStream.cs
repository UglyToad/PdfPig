namespace UglyToad.PdfPig.IO
{
    using System;
    using System.IO;

    /// <summary>
    /// Reads memory as a stream without copying it, unlike a <see cref="MemoryStream"/> also when no array holds it.
    /// </summary>
    internal sealed class ReadOnlyMemoryStream : Stream
    {
        private readonly ReadOnlyMemory<byte> memory;
        private int position;

        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> memory)
        {
            this.memory = memory;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = Math.Min(count, memory.Length - position);
            memory.Span.Slice(position, read).CopyTo(buffer.AsSpan(offset, read));
            position += read;

            return read;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
