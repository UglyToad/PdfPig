namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Util;

    /// <summary>
    /// Provides access to the raw bytes of this Compact Font Format file with utility methods for reading data types from it.
    /// </summary>
    internal class CompactFontFormatData
    {
        private readonly IReadOnlyList<byte> dataBytes;

        public int Position { get; private set; } = -1;

        public int Length => dataBytes.Count;

        [DebuggerStepThrough]
        public CompactFontFormatData(IReadOnlyList<byte> dataBytes)
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

            if (Position >= dataBytes.Count)
            {
                throw new IndexOutOfRangeException($"Cannot read byte at position {Position} of an array which is {dataBytes.Count} bytes long.");
            }

            return dataBytes[Position];
        }

        public byte Peek()
        {
            return dataBytes[Position + 1];
        }

        public bool CanRead()
        {
            return Position < dataBytes.Count - 1;
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

        public CompactFontFormatData SnapshotPortion(int startLocation, int length)
        {
            if (length == 0)
            {
                return new CompactFontFormatData(EmptyArray<byte>.Instance);
            }

            if (startLocation > dataBytes.Count - 1 || startLocation + length > dataBytes.Count)
            {
                throw new ArgumentException($"Attempted to create a snapshot of an invalid portion of the data. Length was {dataBytes.Count}, requested start: {startLocation} and requested length: {length}.");
            }

            var newData = new byte[length];
            var newI = 0;
            for (var i = startLocation; i < startLocation + length; i++)
            {
                newData[newI] = dataBytes[i];
                newI++;
            }

            return new CompactFontFormatData(newData);
        }
    }
}