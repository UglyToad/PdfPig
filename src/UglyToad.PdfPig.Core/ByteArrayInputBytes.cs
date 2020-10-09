namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <inheritdoc />
    /// <summary>
    /// Input bytes from a byte array.
    /// </summary>
    public class ByteArrayInputBytes : IInputBytes
    {
        private readonly int upperBound;
        private readonly byte[] bytes;

        /// <summary>
        /// Bytes
        /// </summary>
        public IReadOnlyList<byte> Bytes => bytes;

        /// <summary>
        /// Create a new <see cref="ByteArrayInputBytes"/>.
        /// </summary>
        [DebuggerStepThrough]
        public ByteArrayInputBytes(IReadOnlyList<byte> bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes is byte[] arr)
            {
                this.bytes = arr;
            }
            else
            {
                this.bytes = bytes.ToArray();
            }

            upperBound = this.bytes.Length - 1;

            currentOffset = -1;
        }

        /// <summary>
        /// Create a new <see cref="ByteArrayInputBytes"/>.
        /// </summary>
        [DebuggerStepThrough]
        public ByteArrayInputBytes(byte[] bytes)
        {
            this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            currentOffset = -1;
            upperBound = bytes.Length - 1;
        }

        private int currentOffset;
        /// <inheritdoc />
        public long CurrentOffset => currentOffset + 1;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (currentOffset == upperBound)
            {
                return false;
            }

            currentOffset++;
            CurrentByte = bytes[currentOffset];
            return true;
        }

        /// <inheritdoc />
        public byte CurrentByte { get; private set; }

        /// <inheritdoc />
        public long Length => bytes.Length;

        /// <inheritdoc />
        public byte? Peek()
        {
            if (currentOffset == upperBound)
            {
                return null;
            }

            return bytes[currentOffset + 1];
        }

        /// <inheritdoc />
        public bool IsAtEnd()
        {
            return currentOffset == upperBound;
        }

        /// <inheritdoc />
        public void Seek(long position)
        {
            currentOffset = (int)position - 1;
            CurrentByte = currentOffset < 0 ? (byte)0 : bytes[currentOffset];
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

            var viableLength = (bytes.Length - currentOffset - 1);
            var readLength = viableLength < bytesToRead ? viableLength : bytesToRead;
            var startFrom = currentOffset + 1;

            Array.Copy(bytes, startFrom, buffer, 0, readLength);
            
            if (readLength > 0)
            {
                currentOffset += readLength;
                CurrentByte = buffer[readLength - 1];
            }

            return readLength;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}