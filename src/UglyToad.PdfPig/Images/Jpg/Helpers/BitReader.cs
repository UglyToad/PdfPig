namespace UglyToad.PdfPig.Images.Jpg.Helpers
{
    using System;    
    using System.IO;
    using System.Diagnostics;
    using JpegMarker = Parts.JpegMarker;

    internal class BitReader : IDisposable
    {
        //private int count = 0;
        //private int index = 0;
        internal Stream stream;
        const byte FrameMarker = 255;

        public BitReader(byte[] data) { stream = new MemoryStream(data); }
        public BitReader(Stream stream) { this.stream = stream; }
         
      
        internal int EnsureData(int bitCount)
        {
            //int todo = ((bitCount - count) + 7) / 8;
            //for (int i = 0; i < todo; i++)
            //{
            //    byte newbyte;
            //    if (stream.Position == stream.Length)
            //    {
            //        // readed the end of the stream. pad bit request with 0xff 
            //        newbyte = 0xff;
            //        index = index << 8; // Roll left one byte. Least most byte (lsb) 'available' as a 'free slot' for new byte.
            //        index |= newbyte;   // 'place' new byte in the 'free slot' 
            //        count += 8;
            //        continue;
            //    }
            //    newbyte = (byte)stream.ReadByte();
            //    index = index << 8; // Roll left one byte. Least most byte (lsb) 'available' as a 'free slot' for new byte.
            //    index |= newbyte;   // 'place' new byte in the 'free slot' 
            //    count += 8;
            //    if (newbyte == FrameMarker) // Check for new frame marker (ending data from old frame)                 
            //    {
            //        if (stream.Position != stream.Length)
            //        {
            //            var marker = (byte)stream.ReadByte();
            //            switch (marker)
            //            {
            //                case 0x00:
            //                case 0xFF:
            //                    break;
            //                case (byte)JpegMarker.EndOfImage: break;
            //                default:
            //                    if ((marker & 0xF8) != 0xD0)
            //                        throw new Exception(); // Syntax
            //                    else
            //                    {
            //                        index = (index << 8) | marker;
            //                        count += 8;
            //                    }
            //                    break;
            //            }
            //        }
            //        else
            //        {
            //            throw new Exception(); // Syntax
            //        }
            //    }
            //}
            //var ret = (index >> (count - bitCount)) & ((1 << bitCount) - 1);            
            //return ret;
            return ShowBits(bitCount);
        }

        //public int Peek(int bitCount)
        //{
        //    EnsureData(bitCount);
        //    int mask = ((1 << count) - 1) ^ ((1 << (count - bitCount)) - 1);
        //    return (int)(index & mask) >> (count - bitCount);
        //}

        public int Read(int bitCount)
        {
            //EnsureData(bitCount);
            //int mask = ((1 << count) - 1) ^ ((1 << (count - bitCount)) - 1);
            //int val = (int)(index & mask) >> (count - bitCount);
            //count -= bitCount;
            //return val;
            return GetBits(bitCount);
        }

        public void Skip(int bitCount)
        {
            //EnsureData(bitCount);
            //count -= bitCount;
            SkipBits(bitCount);
        }
         
        #region Implementation2
        public int BufBits;
        public int Buf;

        public void Align()
        {
            BufBits &= 0xF8;
        }
        private void SkipBits( int bits)
        {
            if (BufBits < bits) { ShowBits(bits); }
            BufBits -= bits;
        }

        private int GetBits(  int bits)
        {
            int res = ShowBits( bits);
            SkipBits( bits);
            return res;
        }

        private int ShowBits( int bits)
        {
            byte newbyte;
            if (bits == 0) { return 0; }

            while (BufBits < bits)
            {
                if (stream.Position == stream.Length)
                {
                    Buf = (Buf << 8) | 0xFF;
                    BufBits += 8;
                    continue;
                }

                newbyte = (byte)stream.ReadByte(); 
                
                BufBits += 8;
                Buf = (Buf << 8) | newbyte;

                if (newbyte == 0xFF)
                {
                    if (stream.Position != stream.Length)
                    {
                        byte marker = (byte)stream.ReadByte();
 
                        switch (marker)
                        {
                            case 0x00:
                            case 0xFF:
                                break;

                            case 0xD9:
                                //data.Remaining = 0;
                                break;

                            default:
                                if ((marker & 0xF8) != 0xD0) { throw new Exception(); }
                                else
                                {
                                    Buf = (Buf << 8) | marker;
                                    BufBits += 8;
                                }
                                break;
                        }
                    }
                    else { throw new Exception(); }
                }
            }

            return (Buf >> (BufBits - bits)) & ((1 << bits) - 1);
        }
        #endregion

        public void Dispose()
        {
            if (stream == null)
            {
                return;
            }

            stream.Dispose();
        }
    }
}
