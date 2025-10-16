namespace UglyToad.PdfPig.Core
{
    using System;
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Input bytes from a stream.
    /// </summary>
    public sealed class StreamInputBytes : IInputBytes
    {
        private readonly Stream stream;
        private readonly bool shouldDispose;
        private byte? peekByte;

        private bool isAtEnd;

        /// <inheritdoc />
        public long CurrentOffset => peekByte.HasValue ? stream.Position - 1 : stream.Position;

        /// <inheritdoc />
        public byte CurrentByte { get; private set; }

        /// <inheritdoc />
        public long Length => stream.Length;

        /// <summary>
        /// Create a new <see cref="StreamInputBytes"/>.
        /// </summary>
        /// <param name="stream">The stream to use, should be readable and seekable.</param>
        /// <param name="shouldDispose">Whether this class should dispose the stream once finished.</param>
        public StreamInputBytes(Stream stream, bool shouldDispose = true)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("The provided stream did not support reading.", nameof(stream));
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("The provided stream did not support seeking.", nameof(stream));
            }

            this.stream = stream;
            this.shouldDispose = shouldDispose;
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            var b = peekByte ?? stream.ReadByte();
            peekByte = null;

            if (b == -1)
            {
                isAtEnd = true;
                CurrentByte = 0;
                return false;
            }

            CurrentByte = (byte) b;
            return true;
        }

        /// <inheritdoc />
        public byte? Peek()
        {
            if (!peekByte.HasValue)
            {
                var v = stream.ReadByte();

                if (v >= 0)
                {
                    peekByte = (byte)v;
                }
                else
                {
                    return null;
                }
            }

            return peekByte;
        }

        /// <inheritdoc />
        public bool IsAtEnd()
        {
            return isAtEnd;
        }

        /// <inheritdoc />
        public void Seek(long position)
        {
            var current = CurrentOffset;
            if (position == current)
            {
                return;
            }
            else if (peekByte.HasValue && position == current + 1)
            {
                MoveNext();
                return;
            }

            isAtEnd = false;
            peekByte = null;

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

        /// <inheritdoc />
        public int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return 0;
            }
            else if (peekByte.HasValue)
            {
                buffer[0] = peekByte.Value;
                peekByte = null;

                return Read(buffer.Slice(1)) + 1;
            }

            int read = stream.Read(buffer);
            if (read > 0)
            {
                CurrentByte = buffer[read - 1];
            }

            isAtEnd = stream.Position == stream.Length;
            
            return read;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (shouldDispose)
            {
                stream?.Dispose();
            }
        }
    }
}
