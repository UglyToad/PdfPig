namespace UglyToad.PdfPig.IO
{
    using System;
    using System.IO;

    internal class StreamInputBytes : IInputBytes
    {
        private readonly Stream stream;
        private readonly bool shouldDispose;

        private bool isAtEnd;

        public long CurrentOffset => stream.Position;

        public byte CurrentByte { get; private set; }

        public long Length => stream.Length;

        public StreamInputBytes(Stream stream, bool shouldDispose = true)
        {
            if (stream == null)
            {
                throw new ArgumentNullException();
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("The provided stream did not support reading.");
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("The provided stream did not support seeking.");
            }

            this.stream = stream;
            this.shouldDispose = shouldDispose;
        }

        public bool MoveNext()
        {
            var b = stream.ReadByte();

            if (b == -1)
            {
                isAtEnd = true;
                CurrentByte = 0;
                return false;
            }

            CurrentByte = (byte) b;
            return true;
        }

        public byte? Peek()
        {
            var current = CurrentOffset;

            var b = stream.ReadByte();

            stream.Seek(current, SeekOrigin.Begin);

            if (b == -1)
            {
                return null;
            }

            return (byte)b;
        }

        public bool IsAtEnd()
        {
            return isAtEnd;
        }

        public void Seek(long position)
        {
            if (position == 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
                CurrentByte = 0;
            }
            else
            {
                stream.Position = position - 1;
                MoveNext();
            }
        }

        public void Dispose()
        {
            if (shouldDispose)
            {
                stream?.Dispose();
            }
        }
    }
}
