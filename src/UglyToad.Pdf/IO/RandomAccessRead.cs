namespace UglyToad.Pdf.IO
{
    using System;

    internal interface IRandomAccessRead : IDisposable
    {
        /**
         * Read a single byte of data.
         *
         * @return The byte of data that is being read.
         *
         * @throws IOException If there is an error while reading the data.
         */
        int Read();

        /**
         * Read a buffer of data.
         *
         * @param b The buffer to write the data to.
         * @return The number of bytes that were actually read.
         * @throws IOException If there was an error while reading the data.
         */
        int Read(byte[]
            b);

        /**
         * Read a buffer of data.
         *
         * @param b The buffer to write the data to.
         * @param offset Offset into the buffer to start writing.
         * @param length The amount of data to attempt to read.
         * @return The number of bytes that were actually read.
         * @throws IOException If there was an error while reading the data.
         */
        int Read(byte[]
            b, int offset, int length);

        /**
         * Returns offset of next byte to be returned by a read method.
         * 
         * @return offset of next byte which will be returned with next {@link #read()}
         *         (if no more bytes are left it returns a value &gt;= length of source)
         *         
         * @throws IOException 
         */
        long GetPosition();

        /**
         * Seek to a position in the data.
         *
         * @param position The position to seek to.
         * @throws IOException If there is an error while seeking.
         */

        void Seek(long position);

        /**
         * The total number of bytes that are available.
         *
         * @return The number of bytes available.
         *
         * @throws IOException If there is an IO error while determining the
         * length of the data stream.
         */
        long Length();

        /**
         * Returns true if this stream has been closed.
         */
        bool IsClosed();

        /**
         * This will peek at the next byte.
         *
         * @return The next byte on the stream, leaving it as available to read.
         *
         * @throws IOException If there is an error reading the next byte.
         */
        int Peek();

        /**
         * Seek backwards the given number of bytes.
         * 
         * @param bytes the number of bytes to be seeked backwards
         * @throws IOException If there is an error while seeking
         */
        void Rewind(int bytes);

        /**
         * Reads a given number of bytes.
         * @param length the number of bytes to be read
         * @return a byte array containing the bytes just read
         * @throws IOException if an I/O error occurs while reading data
         */
        byte[]
            ReadFully(int length);

        /**
         * A simple test to see if we are at the end of the data.
         *
         * @return true if we are at the end of the data.
         *
         * @throws IOException If there is an error reading the next byte.
         */
        bool IsEof();

        /**
         * Returns an estimate of the number of bytes that can be read.
         *
         * @return the number of bytes that can be read
         * @throws IOException if this random access has been closed
         */
        int Available();

        void ReturnToBeginning();

        void Unread(int b);

        void Unread(byte[] bytes);

        void Unread(byte[] bytes, int start, int length);
    }
}