namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Input bytes from a stream.
    /// </summary>
    public class StreamInputBytes : IInputBytes
    {
        private readonly Stream stream;
        private readonly bool shouldDispose;

        private bool isAtEnd;

        /// <summary>
        /// Bytes
        /// </summary>
        public IReadOnlyList<byte> Bytes => throw new NotImplementedException();

        /// <inheritdoc />
        public long CurrentOffset => stream.Position;

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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool IsAtEnd()
        {
            return isAtEnd;
        }

        /// <inheritdoc />
        public void Seek(long position)
        {
            isAtEnd = false;

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
        public int Read(byte[] buffer, int? length = null)
        {
            var bytesToRead = buffer.Length;
            if (length.HasValue)
            {
                if (length.Value < 0)
                {
                    throw new ArgumentOutOfRangeException($"Cannot use a negative length: {length.Value}.");
                }

                if (length.Value > bytesToRead)
                {
                    throw new ArgumentOutOfRangeException($"Cannot read more bytes {length.Value} than there is space in the buffer {buffer.Length}.");
                }

                bytesToRead = length.Value;
            }

            if (bytesToRead == 0)
            {
                return 0;
            }

            var read = stream.Read(buffer, 0, bytesToRead);
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
