namespace UglyToad.PdfPig.Images.Jpg
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
     
    internal class JpgBinaryStreamReader : BinaryReader
    {
        //private static bool isOnDebug = false;
        private long remaining;

        public JpgBinaryStreamReader(Stream stream) : base(stream) { this.remaining = stream.Length; }


        public class JpgBinaryStreamExhaustedException:Exception
        {
           
            public JpgBinaryStreamReader reader { get; private set; }

            public JpgBinaryStreamExhaustedException(JpgBinaryStreamReader reader, string message) : base(message)  
            {
                this.reader = reader;
            }

            public JpgBinaryStreamExhaustedException(JpgBinaryStreamReader reader)
            {
                this.reader = reader;
            }
            
        }
            


        public long Seek(long offset, SeekOrigin origin)
        {
            return base.BaseStream.Seek(offset, origin);
        }

        public void Skip(int n)
        {
            Seek(n, SeekOrigin.Current);
        }

        public void JumpBack(int n)
        {
            Seek(-1 * n, SeekOrigin.Current);            
        }

        public long Remaining => base.BaseStream.Length - Seek(0, SeekOrigin.Current);

        public bool isAtEnd => base.PeekChar() == -1;

        public new byte ReadByte()
        {
            return ReadByteOrThrow();
        }

        public byte ReadByteOrThrow()
        {
            int i = base.ReadByte();
            if (i == -1)
            {
                throw new JpgBinaryStreamExhaustedException(this, "ReadByte()");
            }

            return (byte)i;
        }
        public short ReadShort()
        {
            byte msb = ReadByteOrThrow();            
            byte lsb = ReadByteOrThrow();
            return (short)((msb << 8) + lsb);
        }

        public Int16 ReadInt16BE() => ReadShort();  
        

        public class FrameHeader
        {
            JpgBinaryStreamReader reader;
            public long startOfBlockOffset { get; private set; }
            public long pos => reader.Seek(0, SeekOrigin.Current);
            public int length { get; private set; }  //  Lf - Frame header length
            public int remaining { get;private set; }

            public FrameHeader(JpgBinaryStreamReader reader)
            {
                this.reader = reader;
                startOfBlockOffset = reader.Seek(0, SeekOrigin.Current);
                length = reader.ReadShort();
                length -= 2;
                remaining = length;
                //if (isOnDebug) { Debug.WriteLine($"JpgBinaryStreamReader.FrameHeader() buffer offset: {startOfBlockOffset} length{length}");}
            }

            public void Skip(int n)
            {                
                if (n>remaining) throw new Exception(); // Frame exhausted
                if (n > reader.remaining) throw new Exception(); // Frame exhausted
                reader.Skip(n);
                remaining -= n; // remaining in block 
            }

            public byte ReadByte()
            {
                remaining -= 1;
                if (remaining < 0) throw new Exception();
                return (byte)reader.ReadByteOrThrow();
            }

            public short ReadShort()
            {
                remaining -= 2;
                if (remaining < 0) throw new Exception();
                return reader.ReadShort();
            }
            public Int16 ReadInt16BE() => ReadShort();
        }
         
        public FrameHeader DecodeFrameHeader()
        {            
            return new FrameHeader(this);
        }
    } 
}
