using System;

namespace UglyToad.Pdf.IO
{
    public interface SequentialSource : IDisposable
    {
        /**
         * Read a single byte of data.
         *
         * @return The byte of data that is being read.
         * @throws IOException If there is an error while reading the data.
         */
        int read();

        /**
         * Read a buffer of data.
         *
         * @param b The buffer to write the data to.
         * @return The number of bytes that were actually read.
         * @throws IOException If there was an error while reading the data.
         */
        int read(byte[] b);

        /**
         * Read a buffer of data.
         *
         * @param b The buffer to write the data to.
         * @param offset Offset into the buffer to start writing.
         * @param length The amount of data to attempt to read.
         * @return The number of bytes that were actually read.
         * @throws IOException If there was an error while reading the data.
         */
        int read(byte[] b, int offset, int length);

        /**
         * Returns offset of next byte to be returned by a read method.
         *
         * @return offset of next byte which will be returned with next {@link #read()} (if no more 
         * bytes are left it returns a value &gt;= length of source).
         * @throws IOException If there was an error while reading the data.
         */
        long getPosition();

        /**
         * This will peek at the next byte.
         *
         * @return The next byte on the stream, leaving it as available to read.
         * @throws IOException If there is an error reading the next byte.
         */
        int peek();

        /**
         * Unreads a single byte.
         *
         * @param b byte array to push back
         * @throws IOException if there is an error while unreading
         */
        void unread(int b);

        /**
         * Unreads an array of bytes.
         *
         * @param bytes byte array to be unread
         * @throws IOException if there is an error while unreading
         */
        void unread(byte[] bytes);

        /**
         * Unreads a portion of an array of bytes.
         *
         * @param bytes byte array to be unread
         * @param start start index
         * @param len number of bytes to be unread
         * @throws IOException if there is an error while unreading
         */
        void unread(byte[] bytes, int start, int len);

        /**
         * Reads a given number of bytes in its entirety.
         *
         * @param length the number of bytes to be read
         * @return a byte array containing the bytes just read
         * @throws IOException if an I/O error occurs while reading data
         */
        byte[] readFully(int length);

        /**
         * Returns true if the end of the data source has been reached.
         *
         * @return true if we are at the end of the data.
         * @throws IOException If there is an error reading the next byte.
         */
        bool isEOF();
    }

    public class BufferSequentialSource : SequentialSource
    {
        private readonly IRandomAccessRead reader;

        /**
         * Constructor.
         * 
         * @param reader The random access reader to wrap.
         */
        public BufferSequentialSource(IRandomAccessRead reader)
        {
            this.reader = reader;
        }

        public int read()
        {
            return reader.Read();
        }

        public int read(byte[] b)
        {
            return reader.Read(b);
        }

        public int read(byte[] b, int offset, int length)
        {
            return reader.Read(b, offset, length);
        }

        public long getPosition()
        {
            return reader.GetPosition();
        }

        public int peek()
        {
            return reader.Peek();
        }

        public void unread(int b)
        {
            reader.Rewind(1);
        }

        public void unread(byte[] bytes)
        {
            reader.Rewind(bytes.Length);
        }

        public void unread(byte[] bytes, int start, int len)
        {
            reader.Rewind(len - start);
        }

        public byte[] readFully(int length)
        {
            return reader.ReadFully(length);
        }

        public bool isEOF()
        {
            return reader.IsEof();
        }

        public void Dispose()
        {
            reader.Dispose();
        }
    }
}
