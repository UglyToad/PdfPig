using System;

namespace UglyToad.Pdf.IO
{
    using System.IO;

    public interface IInputStream : IDisposable
    {
        int read();

        int read(byte[] b);

        int read(byte[] b, int off, int len);

        long available();
    }

    public interface IOutputStream : IDisposable
    {
        /// <summary>
        /// Flushes this output stream and forces any buffered output bytes to be written out.
        /// </summary>
        void flush();

        /// <summary>
        /// Writes b.length bytes from the specified byte array to this output stream.
        /// </summary>
        void write(byte[] b);

        /// <summary>
        /// Writes len bytes from the specified byte array starting at offset off to this output stream.
        /// </summary>
        void write(byte[] b, int off, int len);

        /// <summary>
        /// Writes the specified byte to this output stream.
        /// </summary>
        void write(int b);
    }

    public class BinaryInputStream : IInputStream
    {
        private readonly MemoryStream memoryStream;
        private readonly BinaryReader reader;

        public BinaryInputStream(byte[] bytes)
        {
            memoryStream = new MemoryStream(bytes);
            reader = new BinaryReader(memoryStream);
        }

        public BinaryInputStream()
        {
            memoryStream = new MemoryStream();
            reader = new BinaryReader(memoryStream);
        }
        
        public int read()
        {
            return reader.Read();
        }

        public int read(byte[] b)
        {
            return read(b, 0, b.Length);
        }

        public int read(byte[] b, int off, int len)
        {
            return reader.Read(b, off, len);
        }

        public long available()
        {
            return memoryStream.Length - memoryStream.Position;
        }

        public void Dispose()
        {
            reader.Dispose();
            memoryStream.Dispose();
        }
    }

    public class BinaryOutputStream : IOutputStream
    {
        private readonly MemoryStream memoryStream = new MemoryStream();
        private readonly BinaryWriter writer;

        public BinaryOutputStream()
        {
            writer = new BinaryWriter(memoryStream);
        }

        public void flush()
        {
            writer.Flush();
        }

        public void write(byte[] b)
        {
            writer.Write(b);
        }

        public void write(byte[] b, int off, int len)
        {
            writer.Write(b, len, off);
        }

        public void write(int b)
        {
            writer.Write(b);
        }

        public byte[] ToArray()
        {
            return memoryStream.ToArray();
        }

        public void Dispose()
        {
            writer.Dispose();
            memoryStream.Dispose();
        }
    }
}
