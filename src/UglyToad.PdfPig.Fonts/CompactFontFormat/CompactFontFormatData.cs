namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Core;

    /// <summary>
    /// Provides access to the raw bytes of this Compact Font Format file with utility methods for reading data types from it.
    /// </summary>
    public class CompactFontFormatData
    {
        private readonly ReadOnlyMemory<byte> dataBytes;

        /// <summary>
        /// The current position in the data.
        /// </summary>
        public int Position { get; private set; } = -1;

        /// <summary>
        /// The length of the data.
        /// </summary>
        public int Length => dataBytes.Length;

        /// <summary>
        /// Create a new <see cref="CompactFontFormatData"/>.
        /// </summary>
        [DebuggerStepThrough]
        public CompactFontFormatData(ReadOnlyMemory<byte> dataBytes)
        {
            this.dataBytes = dataBytes;
        }

        /// <summary>
        /// Read a string of the specified length.
        /// </summary>
        public string ReadString(int length, Encoding encoding)
        {
            var bytes = new byte[length];

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = ReadByte();
            }

            return encoding.GetString(bytes);
        }

        /// <summary>
        /// Read Card8 format.
        /// </summary>
        public byte ReadCard8()
        {
            return ReadByte();
        }

        /// <summary>
        /// Read Card16 format.
        /// </summary>
        public ushort ReadCard16()
        {
            return (ushort)(ReadByte() << 8 | ReadByte());
        }

        /// <summary>
        /// Read Offsize.
        /// </summary>
        public byte ReadOffsize()
        {
            return ReadByte();
        }

        /// <summary>
        /// Read Offset.
        /// </summary>
        public int ReadOffset(int offsetSize)
        {
            var value = 0;

            for (var i = 0; i < offsetSize; i++)
            {
                value = value << 8 | ReadByte();
            }

            return value;
        }

        /// <summary>
        /// Read byte.
        /// </summary>
        public byte ReadByte()
        {
            Position++;

            if (Position >= dataBytes.Length)
            {
                throw new IndexOutOfRangeException($"Cannot read byte at position {Position} of an array which is {dataBytes.Length} bytes long.");
            }

            return dataBytes.Span[Position];
        }

        /// <summary>
        /// Peek the next byte without advancing the data.
        /// </summary>
        public byte Peek()
        {
            return dataBytes.Span[Position + 1];
        }

        /// <summary>
        /// Whether there's more data to read in the input.
        /// </summary>
        public bool CanRead()
        {
            return Position < dataBytes.Length - 1;
        }

        /// <summary>
        /// Move to the given offset from the beginning.
        /// </summary>
        public void Seek(int offset)
        {
            Position = offset - 1;
        }

        /// <summary>
        /// Read long.
        /// </summary>
        public long ReadLong()
        {
            return (ReadCard16() << 16) | ReadCard16();
        }

        /// <summary>
        /// Read sid.
        /// </summary>
        public int ReadSid()
        {
            return ReadByte() << 8 | ReadByte();
        }

        /// <summary>
        /// Read byte array of given length.
        /// </summary>
        public byte[] ReadBytes(int length)
        {
            var result = new byte[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = ReadByte();
            }

            return result;
        }

        /// <summary>
        /// Create a new <see cref="CompactFontFormatData"/> from this data with a snapshot at the position and length.
        /// </summary>
        public CompactFontFormatData SnapshotPortion(int startLocation, int length)
        {
            if (length == 0)
            {
                return new CompactFontFormatData(Array.Empty<byte>());
            }

            if (startLocation > dataBytes.Length - 1 || startLocation + length > dataBytes.Length)
            {
                throw new ArgumentException($"Attempted to create a snapshot of an invalid portion of the data. Length was {dataBytes.Length}, requested start: {startLocation} and requested length: {length}.");
            }

            var newData = new byte[length];
            var newI = 0;
            for (var i = startLocation; i < startLocation + length; i++)
            {
                newData[newI] = dataBytes.Span[i];
                newI++;
            }

            return new CompactFontFormatData(newData);
        }
    }
}