namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Text;
    using Util;

    internal class CompactFontFormatParser
    {
        private const string TagOtto = "OTTO";
        private const string TagTtcf = "ttcf";
        private const string TagTtfonly = "\u0000\u0001\u0000\u0000";

        private readonly CompactFontFormatIndividualFontParser individualFontParser;

        public CompactFontFormatParser(CompactFontFormatIndividualFontParser individualFontParser)
        {
            this.individualFontParser = individualFontParser;
        }

        public void Parse(CompactFontFormatData data)
        {
            var tag = ReadTag(data);

            switch (tag)
            {
                case TagOtto:
                    throw new NotImplementedException("Currently tagged CFF data is not supported.");
                case TagTtcf:
                    throw new NotSupportedException("True Type Collection fonts are not supported.");
                case TagTtfonly:
                    throw new NotSupportedException("OpenType fonts containing a true type font are not supported.");
                default:
                    data.Seek(0);
                    break;
            }

            var header = ReadHeader(data);

            var fontNames = ReadStringIndex(data);

            var topLevelDict = ReadDictionaryData(data);

            var stringIndex = ReadStringIndex(data);

            var globalSubroutineIndex = ReadDictionaryData(data);

            for (var i = 0; i < fontNames.Length; i++)
            {
                var fontName = fontNames[i];

                individualFontParser.Parse(data, fontName, topLevelDict[i], stringIndex);
            }
        }

        private static string ReadTag(CompactFontFormatData data)
        {
            var tag = data.ReadString(4, OtherEncodings.Iso88591);

            return tag;
        }

        private static CompactFontFormatHeader ReadHeader(CompactFontFormatData data)
        {
            var major = data.ReadCard8();
            var minor = data.ReadCard8();
            var headerSize = data.ReadCard8();
            var offsetSize = data.ReadOffsize();

            return new CompactFontFormatHeader(major, minor, headerSize, offsetSize);
        }

        /// <summary>
        /// Reads indexed string data.
        /// </summary>
        private static string[] ReadStringIndex(CompactFontFormatData data)
        {
            var index = ReadIndex(data);

            var count = index.Length - 1;

            var result = new string[count];

            for (var i = 0; i < count; i++)
            {
                var length = index[i + 1] - index[i];

                if (length < 0)
                {
                    throw new InvalidOperationException($"Negative object length {length} at {i}. Current position: {data.Position}.");
                }

                result[i] = data.ReadString(length, OtherEncodings.Iso88591);
            }

            return result;
        }

        private static byte[][] ReadDictionaryData(CompactFontFormatData data)
        {
            var index = ReadIndex(data);

            var count = index.Length - 1;

            var results = new byte[count][];

            for (var i = 0; i < count; i++)
            {
                var length = index[i + 1] - index[i];

                if (length < 0)
                {
                    throw new InvalidOperationException($"Negative object length {length} at {i}. Current position: {data.Position}.");
                }

                results[i] = data.ReadBytes(length);
            }

            return results;
        }

        private static int[] ReadIndex(CompactFontFormatData data)
        {
            var count = data.ReadCard16();

            var offsetSize = data.ReadOffsize();

            var offsets = new int[count + 1];

            for (var i = 0; i < offsets.Length; i++)
            {
                offsets[i] = data.ReadOffset(offsetSize);
            }

            return offsets;
        }
    }

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

    /// <summary>
    /// The header table for the binary data of a CFF file.
    /// </summary>
    internal struct CompactFontFormatHeader
    {
        public byte MajorVersion { get; }

        public byte MinorVersion { get; }

        public byte SizeInBytes { get; }

        public byte OffsetSize { get; }

        public CompactFontFormatHeader(byte majorVersion, byte minorVersion, byte sizeInBytes, byte offsetSize)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            SizeInBytes = sizeInBytes;
            OffsetSize = offsetSize;
        }

        public override string ToString()
        {
            return $"Major: {MajorVersion}, Minor: {MinorVersion}, Header Size: {SizeInBytes}, Offset: {OffsetSize}";
        }
    }
}
