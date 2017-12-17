namespace UglyToad.Pdf.IO
{
    using System.Collections.Generic;

    public class ByteArrayInputBytes : IInputBytes
    {
        private readonly IReadOnlyList<byte> bytes;

        public ByteArrayInputBytes(IReadOnlyList<byte> bytes)
        {
            this.bytes = bytes;
            CurrentOffset = -1;
        }

        public int CurrentOffset { get; private set; }

        public bool MoveNext()
        {
            if (CurrentOffset == bytes.Count - 1)
            {
                return false;
            }

            CurrentOffset++;
            CurrentByte = bytes[CurrentOffset];
            return true;
        }

        public byte CurrentByte { get; private set; }

        public byte? Peek()
        {
            if (CurrentOffset == bytes.Count - 1)
            {
                return null;
            }

            return bytes[CurrentOffset + 1];
        }

        public bool IsAtEnd()
        {
            return CurrentOffset == bytes.Count - 1;
        }

        public void Seek(long position)
        {
            CurrentOffset = (int)position;
            CurrentByte = CurrentOffset < 0 ? (byte)0 : bytes[CurrentOffset];
        }
    }
}