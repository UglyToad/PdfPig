namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.IO;
    using System.Text;
    using IO;
    using PdfPig.Exceptions;

    internal class TrueTypeDataBytes
    {
        private readonly byte[] internalBuffer = new byte[16];
        private readonly IInputBytes inputBytes;

        public TrueTypeDataBytes(IInputBytes inputBytes)
        {
            this.inputBytes = inputBytes;
        }

        public long Position => inputBytes.CurrentOffset;

        public float Read32Fixed()
        {
            float retval = ReadSignedShort();
            retval += (ReadUnsignedShort() / 65536.0f);
            return retval;
        }

        public short ReadSignedShort()
        {
            ReadBuffered(internalBuffer, 2);

            return unchecked((short)((internalBuffer[0] << 8) + (internalBuffer[1] << 0)));
        }

        public ushort ReadUnsignedShort()
        {
            ReadBuffered(internalBuffer, 2);

            return (ushort)((internalBuffer[0] << 8) + (internalBuffer[1] << 0));
        }
        
        private void ReadBuffered(byte[] buffer, int length)
        {
            var read = inputBytes.Read(buffer, length);
            if (read < length)
            {
                throw new EndOfStreamException($"Could not read a buffer of {length} bytes.");
            }
        }

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

        public string ReadString(int bytesToRead, Encoding encoding)
        {
            byte[] data = new byte[bytesToRead];
            ReadBuffered(data, bytesToRead);
            return encoding.GetString(data, 0, data.Length);
        }

        public long ReadUnsignedInt()
        {
            ReadBuffered(internalBuffer, 4);

            return ((long)internalBuffer[0] << 24) + ((long)internalBuffer[1] << 16) + (internalBuffer[2] << 8) + (internalBuffer[3] << 0);
        }

        public int ReadSignedInt()
        {
            ReadBuffered(internalBuffer, 4);

            return (internalBuffer[0] << 24) + (internalBuffer[1] << 16) + (internalBuffer[2] << 8) + (internalBuffer[3] << 0);
        }

        public long ReadLong()
        {
            var upper = (long)ReadSignedInt();
            var lower = ReadSignedInt();
            var result = (upper << 32) + (lower & 0xFFFFFFFF);
            return result;
        }

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
                throw new PdfDocumentFormatException($"Invalid date offset ({secondsSince1904} seconds) encountered in TrueType header table.");
            }
        }

        public void Seek(long position)
        {
            inputBytes.Seek(position);
        }

        public int ReadSignedByte()
        {
            ReadBuffered(internalBuffer, 1);

            var signedByte = internalBuffer[0];

            return signedByte < 127 ? signedByte : signedByte - 256;
        }

        public ushort[] ReadUnsignedShortArray(int length)
        {
            var result = new ushort[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = ReadUnsignedShort();
            }

            return result;
        }

        public byte[] ReadByteArray(int length)
        {
            var result = new byte[length];

            ReadBuffered(result, length);

            return result;
        }

        public void ReadUnsignedIntArray(long[] offsets, int length)
        {
            for (int i = 0; i < length; i++)
            {
                offsets[i] = ReadUnsignedInt();
            }
        }

        public short[] ReadShortArray(int length)
        {
            var result = new short[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = ReadSignedShort();
            }

            return result;
        }
    }
}
