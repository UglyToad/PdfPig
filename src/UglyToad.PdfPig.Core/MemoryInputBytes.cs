namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Diagnostics;

    /// <inheritdoc />
    /// <summary>
    /// Input bytes from a byte array.
    /// </summary>
    public sealed class MemoryInputBytes : IInputBytes
    {
        private readonly int upperBound;
        private readonly ReadOnlyMemory<byte> memory;

        /// <summary>
        /// Create a new <see cref="MemoryInputBytes"/>.
        /// </summary>
        [DebuggerStepThrough]
        public MemoryInputBytes(ReadOnlyMemory<byte> memory)
        {
            this.memory = memory;

            upperBound = this.memory.Length - 1;

            currentOffset = -1;
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
            CurrentByte = memory.Span[currentOffset];
            return true;
        }

        /// <inheritdoc />
        public byte CurrentByte { get; private set; }

        /// <inheritdoc />
        public long Length => memory.Span.Length;

        /// <inheritdoc />
        public byte? Peek()
        {
            if (currentOffset == upperBound)
            {
                return null;
            }

            return memory.Span[currentOffset + 1];
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
            CurrentByte = currentOffset < 0 ? (byte)0 : memory.Span[currentOffset];
        }

        /// <inheritdoc />
        public int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return 0;
            }

            var viableLength = (memory.Length - currentOffset - 1);
            var readLength = viableLength < buffer.Length ? viableLength : buffer.Length;
            var startFrom = currentOffset + 1;

            memory.Span.Slice(startFrom, readLength).CopyTo(buffer);

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