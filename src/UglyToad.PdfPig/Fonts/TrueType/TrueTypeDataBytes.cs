namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.IO;
    using System.Text;
    using IO;

    internal class TrueTypeDataBytes
    {
        private readonly byte[] internalBuffer = new byte[16];
        private readonly IInputBytes inputBytes;

        public TrueTypeDataBytes(IInputBytes inputBytes)
        {
            this.inputBytes = inputBytes;
        }

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

        public int ReadUnsignedShort()
        {
            ReadBuffered(internalBuffer, 2);

            return (internalBuffer[0] << 8) + (internalBuffer[1] << 0);
        }

        public int ReadUnsignedByte()
        {
            ReadBuffered(internalBuffer, 1);

            // TODO: the cast from int -> byte -> int here suggest we are treating data incorrectly.
            return internalBuffer[0];
        }

        private void ReadBuffered(byte[] buffer, int length)
        {
            var numberRead = 0;
            while (numberRead < length)
            {
                if (!inputBytes.MoveNext())
                {
                    throw new EndOfStreamException($"Could not read a buffer of {length} bytes.");
                }

                buffer[numberRead] = inputBytes.CurrentByte;
                numberRead++;
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
            ReadBuffered(internalBuffer, 8);

            var result = FromBytes(internalBuffer, 0, 8);

            return result;
        }

        public DateTime ReadInternationalDate()
        {
            // TODO: this returns the wrong value, investigate...
            long secondsSince1904 = ReadLong();

            var date = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = date.AddSeconds(secondsSince1904);
            result = result.AddMonths(1);
            result = result.AddDays(1);

            return result;
        }

        public void Seek(long position)
        {
            inputBytes.Seek(position);
        }

        private long FromBytes(byte[] buffer, int startIndex, int bytesToConvert)
        {
            long ret = 0;
            for (int i = 0; i < bytesToConvert; i++)
            {
                ret = unchecked((ret << 8) | buffer[startIndex + i]);
            }

            return ret;
        }

        public int ReadSignedByte()
        {
            ReadBuffered(internalBuffer, 1);

            var signedByte = internalBuffer[0];

            return signedByte < 127 ? signedByte : signedByte - 256;
        }

        public int[] ReadUnsignedShortArray(int length)
        {
            var result = new int[length];

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
    }
}
