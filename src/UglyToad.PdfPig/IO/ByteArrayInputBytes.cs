namespace UglyToad.PdfPig.IO
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    internal class ByteArrayInputBytes : IInputBytes
    {
        private readonly byte[] bytes;

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

            currentOffset = -1;
        }

        [DebuggerStepThrough]
        public ByteArrayInputBytes(byte[] bytes)
        {
            this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            currentOffset = -1;
        }

        private long currentOffset;
        public long CurrentOffset => currentOffset + 1;

        public bool MoveNext()
        {
            if (currentOffset == bytes.Length - 1)
            {
                return false;
            }

            currentOffset++;
            CurrentByte = bytes[(int)currentOffset];
            return true;
        }

        public byte CurrentByte { get; private set; }

        public long Length => bytes.Length;

        public byte? Peek()
        {
            if (currentOffset == bytes.Length - 1)
            {
                return null;
            }

            return bytes[(int)currentOffset + 1];
        }

        public bool IsAtEnd()
        {
            return currentOffset == bytes.Length - 1;
        }

        public void Seek(long position)
        {
            currentOffset = (int)position - 1;
            CurrentByte = currentOffset < 0 ? (byte)0 : bytes[(int)currentOffset];
        }

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
            var readLength = (int)(viableLength < bytesToRead ? viableLength : bytesToRead);
            var startFrom = (int)currentOffset + 1;

            Array.Copy(bytes, startFrom, buffer, 0, readLength);
            
            if (readLength > 0)
            {
                currentOffset += readLength;
                CurrentByte = buffer[readLength - 1];
            }

            return readLength;
        }

        public void Dispose()
        {
        }
    }
}