namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Text;

    /// <summary>
    /// Provides access to the raw bytes of this Compact Font Format file with utility methods for reading data types from it.
    /// </summary>
    internal class CompactFontFormatData
    {
        private readonly byte[] dataBytes;

        public int Position { get; private set; } = -1;

        public CompactFontFormatData(byte[] dataBytes)
        {
            this.dataBytes = dataBytes;
        }

        public string ReadString(int length, Encoding encoding)
        {
            var bytes = new byte[length];

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = ReadByte();
            }

            return encoding.GetString(bytes);
        }

        public byte ReadCard8()
        {
            return ReadByte();
        }

        public ushort ReadCard16()
        {
            return (ushort)(ReadByte() << 8 | ReadByte());
        }

        public byte ReadOffsize()
        {
            return ReadByte();
        }

        public int ReadOffset(int offsetSize)
        {
            var value = 0;

            for (var i = 0; i < offsetSize; i++)
            {
                value = value << 8 | ReadByte();
            }

            return value;
        }

        public byte ReadByte()
        {
            Position++;

            if (Position >= dataBytes.Length)
            {
                throw new IndexOutOfRangeException($"Cannot read byte at position {Position} of an array which is {dataBytes.Length} bytes long.");
            }

            return dataBytes[Position];
        }

        public byte Peek()
        {
            return dataBytes[Position + 1];
        }

        public bool CanRead()
        {
            return Position < dataBytes.Length - 1;
        }

        public void Seek(int offset)
        {
            Position = offset - 1;
        }

        public long ReadLong()
        {
            return (ReadCard16() << 16) | ReadCard16();
        }

        public int ReadSid()
        {
            return ReadByte() << 8 | ReadByte();
        }

        public byte[] ReadBytes(int length)
        {
            var result = new byte[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = ReadByte();
            }

            return result;
        }
    }
}