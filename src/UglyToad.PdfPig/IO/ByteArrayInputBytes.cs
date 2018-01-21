namespace UglyToad.PdfPig.IO
{
    using System.Collections.Generic;

    internal class ByteArrayInputBytes : IInputBytes
    {
        private readonly IReadOnlyList<byte> bytes;

        public ByteArrayInputBytes(IReadOnlyList<byte> bytes)
        {
            this.bytes = bytes;
            currentOffset = -1;
        }
        
        private long currentOffset;
        public long CurrentOffset => currentOffset + 1;

        public bool MoveNext()
        {
            if (currentOffset == bytes.Count - 1)
            {
                return false;
            }

            currentOffset++;
            CurrentByte = bytes[(int)currentOffset];
            return true;
        }

        public byte CurrentByte { get; private set; }

        public long Length => bytes.Count;

        public byte? Peek()
        {
            if (currentOffset == bytes.Count - 1)
            {
                return null;
            }

            return bytes[(int)currentOffset + 1];
        }

        public bool IsAtEnd()
        {
            return currentOffset == bytes.Count - 1;
        }

        public void Seek(long position)
        {
            currentOffset = (int)position - 1;
            CurrentByte = currentOffset < 0 ? (byte)0 : bytes[(int)currentOffset];
        }

        public void Dispose()
        {
        }
    }
}