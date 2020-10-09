namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.IO;
    using System.Text;
    using Core;

    /// <summary>
    /// Wraps the <see cref="IInputBytes"/> to support reading TrueType data types.
    /// </summary>
    public class TrueTypeDataBytes
    {
        private readonly byte[] internalBuffer = new byte[16];
        private readonly IInputBytes inputBytes;

        /// <summary>
        /// InputBytes
        /// </summary>
        public IInputBytes InputBytes => inputBytes;

        /// <summary>
        /// Create a new <see cref="TrueTypeDataBytes"/>.
        /// </summary>
        public TrueTypeDataBytes(byte[] bytes) : this(new ByteArrayInputBytes(bytes)) { }

        /// <summary>
        /// Create a new <see cref="TrueTypeDataBytes"/>.
        /// </summary>
        public TrueTypeDataBytes(IInputBytes inputBytes)
        {
            this.inputBytes = inputBytes ?? throw new ArgumentNullException(nameof(inputBytes));
        }

        /// <summary>
        /// The current position in the data.
        /// </summary>
        public long Position => inputBytes.CurrentOffset;

        /// <summary>
        /// The length of the data in bytes.
        /// </summary>
        public long Length => inputBytes.Length;

        /// <summary>
        /// Read a 32-fixed floating point value.
        /// </summary>
        public float Read32Fixed()
        {
            float retval = ReadSignedShort();
            retval += (ReadUnsignedShort() / 65536.0f);
            return retval;
        }

        /// <summary>
        /// Read a <see langword="short"/>.
        /// </summary>
        public short ReadSignedShort()
        {
            ReadBuffered(internalBuffer, 2);

            return unchecked((short)((internalBuffer[0] << 8) + (internalBuffer[1] << 0)));
        }

        /// <summary>
        /// Read a <see langword="ushort"/>.
        /// </summary>
        public ushort ReadUnsignedShort()
        {
            ReadBuffered(internalBuffer, 2);

            return (ushort)((internalBuffer[0] << 8) + (internalBuffer[1] << 0));
        }

        /// <summary>
        /// Read a <see langword="byte"/>.
        /// </summary>
        public byte ReadByte()
        {
            ReadBuffered(internalBuffer, 1);

            return internalBuffer[0];
        }

        /// <summary>
        /// Reads the 4 character tag from the TrueType file.
        /// </summary>
        public string ReadTag()
        {
            return ReadString(4, Encoding.UTF8);
        }

        /// <summary>
        /// Read a <see langword="string"/> of the given number of bytes in length with the specified encoding.
        /// </summary>
        public string ReadString(int bytesToRead, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            byte[] data = new byte[bytesToRead];
            ReadBuffered(data, bytesToRead);
            return encoding.GetString(data, 0, data.Length);
        }

        /// <summary>
        /// Read a <see langword="uint"/>.
        /// </summary>
        public uint ReadUnsignedInt()
        {
            ReadBuffered(internalBuffer, 4);

            return (uint)(((long)internalBuffer[0] << 24) + ((long)internalBuffer[1] << 16) + (internalBuffer[2] << 8) + (internalBuffer[3] << 0));
        }

        /// <summary>
        /// Read an <see langword="int"/>.
        /// </summary>
        public int ReadSignedInt()
        {
            ReadBuffered(internalBuffer, 4);

            return (internalBuffer[0] << 24) + (internalBuffer[1] << 16) + (internalBuffer[2] << 8) + (internalBuffer[3] << 0);
        }

        /// <summary>
        /// Read a <see langword="long"/>.
        /// </summary>
        public long ReadLong()
        {
            var upper = (long)ReadSignedInt();
            var lower = ReadSignedInt();
            var result = (upper << 32) + (lower & 0xFFFFFFFF);
            return result;
        }
        
        /// <summary>
        /// Read a <see cref="DateTime"/> from the data in UTC time.
        /// In TrueType dates are specified as the number of seconds since 1904-01-01.
        /// </summary>
        public DateTime ReadInternationalDate()
        {
            var secondsSince1904 = ReadLong();

            var date = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            try
            {
                var result = date.AddSeconds(secondsSince1904);

                return result;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidFontFormatException($"Invalid date offset ({secondsSince1904} seconds) encountered in TrueType header table.");
            }
        }

        /// <summary>
        /// Move to the specified position in the data.
        /// </summary>
        public void Seek(long position)
        {
            inputBytes.Seek(position);
        }

        /// <summary>
        /// Read an <see langword="int"/> which represents a signed byte.
        /// </summary>
        public int ReadSignedByte()
        {
            ReadBuffered(internalBuffer, 1);

            var signedByte = internalBuffer[0];

            return signedByte < 127 ? signedByte : signedByte - 256;
        }

        /// <summary>
        /// Read an array of <see langword="ushort"/>s with the specified number of values.
        /// </summary>
        public ushort[] ReadUnsignedShortArray(int length)
        {
            var result = new ushort[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = ReadUnsignedShort();
            }

            return result;
        }

        /// <summary>
        /// Read an array of <see langword="byte"/>s with the specified number of values.
        /// </summary>
        public byte[] ReadByteArray(int length)
        {
            var result = new byte[length];

            ReadBuffered(result, length);

            return result;
        }

        /// <summary>
        /// Read an array of <see langword="uint"/>s with the specified number of values.
        /// </summary>
        public uint[] ReadUnsignedIntArray(int length)
        {
            var result = new uint[length];
            for (var i = 0; i < length; i++)
            {
                result[i] = ReadUnsignedInt();
            }

            return result;
        }

        /// <summary>
        /// Read an array of <see langword="short"/>s with the specified number of values.
        /// </summary>
        public short[] ReadShortArray(int length)
        {
            var result = new short[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = ReadSignedShort();
            }

            return result;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"@: {Position} of {inputBytes.Length} bytes.";
        }

        private void ReadBuffered(byte[] buffer, int length)
        {
            var read = inputBytes.Read(buffer, length);
            if (read < length)
            {
                throw new EndOfStreamException($"Could not read a buffer of {length} bytes.");
            }
        }
    }
}
