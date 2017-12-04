namespace UglyToad.Pdf.Fonts.TrueType
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using IO;
    using Util;

    internal class TrueTypeDataBytes
    {
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
            int ch1 = Read();
            int ch2 = Read();
            if ((ch1 | ch2) < 0)
            {
                throw new EndOfStreamException();
            }

            return (short)((ch1 << 8) + (ch2 << 0));
        }

        public int ReadUnsignedShort()
        {
            int ch1 = Read();
            int ch2 = Read();
            if ((ch1 | ch2) < 0)
            {
                throw new EndOfStreamException();
            }

            return (ch1 << 8) + (ch2 << 0);
        }

        public int Read()
        {
            // We're no longer moving because we're at the end.
            if (!inputBytes.MoveNext())
            {
                return -1;
            }

            int result = inputBytes.CurrentByte;

            return (result + 256) % 256;
        }

        public byte[] Read(int numberOfBytes)
        {
            byte[] data = new byte[numberOfBytes];
            int amountRead = 0;

            while (amountRead < numberOfBytes)
            {
                if (!inputBytes.MoveNext())
                {
                    throw new EndOfStreamException();
                }

                data[amountRead] = inputBytes.CurrentByte;
                amountRead++;
            }

            return data;
        }

        public string ReadString(int length)
        {
            return ReadString(length, OtherEncodings.Iso88591);
        }

        public string ReadString(int length, Encoding encoding)
        {
            byte[] buffer = Read(length);

            var str = encoding.GetString(buffer);

            return str;
        }

        public long ReadUnsignedInt()
        {
            long byte1 = Read();
            long byte2 = Read();
            long byte3 = Read();
            long byte4 = Read();

            if (byte4 < 0)
            {
                throw new EndOfStreamException();
            }

            return (byte1 << 24) + (byte2 << 16) + (byte3 << 8) + (byte4 << 0);
        }

        public int ReadSignedInt()
        {
            int ch1 = Read();
            int ch2 = Read();
            int ch3 = Read();
            int ch4 = Read();
            if ((ch1 | ch2 | ch3 | ch4) < 0)
            {
                throw new EndOfStreamException();
            }

            return (ch1 << 24) + (ch2 << 16) + (ch3 << 8) + (ch4 << 0);
        }

        public long ReadLong()
        {
            return (ReadSignedInt() << 32) + (ReadSignedInt() & 0xFFFFFFFFL);
        }

        public DateTime ReadInternationalDate()
        {
            // TODO: this returns the wrong value, investigate...
            long secondsSince1904 = ReadLong();
            
            var date = new DateTime(1904, 1, 1, 0, 0, 0, 0, new GregorianCalendar());

            var result = date.AddSeconds(secondsSince1904);
            result = result.AddMonths(1);
            result = result.AddDays(1);

            return result;
        }

        public void Seek(long position)
        {
            inputBytes.Seek(position);
        }
    }
}
