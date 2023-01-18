namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;

    /// <summary>
    /// A wrapper for an <see cref="IImageInputStream"/> which is able to provide a view of a specific part of the wrapped stream.
    /// Read accesses to the wrapped stream are synchronized, so that users of this stream need to deal with synchronization
    /// against other users of the same instance, but not against other users of the wrapped stream.
    /// </summary>
    internal class SubInputStream : AbstractImageInputStream
    {
        private readonly IImageInputStream wrappedStream;

        // The position in the wrapped stream at which the window starts. Offset is an absolut value.
        private readonly long offset;

        // The length of the window. Length is an relative value.
        private readonly long length;

        // A buffer which is used to improve read performance.
        private readonly byte[] buffer = new byte[4096];

        //Location of the first byte in the buffer with respect to the start of the stream.
        private long bufferBase;

        // Location of the last byte in the buffer with respect to the start of the stream.
        private long bufferTop;

        private long streamPosition;

        /// <inheritdoc />
        public override sealed long Length => length;

        /// <inheritdoc />
        public override sealed long Position => streamPosition;

        /// <summary>
        /// Constructs a new SubInputStream which provides a view of the wrapped stream.
        /// </summary>
        /// <param name="iis">The stream to be wrapped.</param>
        /// <param name="offset">The absolute position in the wrapped stream at which the sub-stream starts.</param>
        /// <param name="length">The length of the sub-stream.</param>
        public SubInputStream(IImageInputStream iis, long offset, long length)
        {
            this.wrappedStream = iis;
            this.offset = offset;
            this.length = length;
        }

        /// <inheritdoc />
        public override sealed int Read()
        {
            if (streamPosition >= length)
            {
                return -1;
            }

            if (streamPosition >= bufferTop || streamPosition < bufferBase)
            {
                if (!FillBuffer())
                {
                    return -1;
                }
            }

            int read = 0xff & buffer[(int)(streamPosition - bufferBase)];

            streamPosition++;

            return read;
        }

        /// <inheritdoc />
        public override sealed int Read(byte[] b, int off, int len)
        {
            if (streamPosition >= length)
            {
                return -1;
            }

            lock (wrappedStream)
            {
                var desiredPosition = streamPosition + offset;
                if (wrappedStream.Position != desiredPosition)
                {
                    wrappedStream.Seek(desiredPosition);
                }

                int toRead = (int)Math.Min(len, length - Position);
                int read = wrappedStream.Read(b, off, toRead);
                streamPosition += read;

                return read;
            }
        }

        /// <inheritdoc />
        public override sealed void Seek(long pos)
        {
            streamPosition = pos;
        }

        /// <inheritdoc />
        public override sealed void Dispose()
        {
            wrappedStream.Dispose();
        }

        /// <summary>
        /// Fill the buffer at the current stream position.
        /// </summary>
        /// <returns>true if successful, false otherwise</returns>
        private bool FillBuffer()
        {
            lock (wrappedStream)
            {
                var desiredPosition = streamPosition + offset;
                if (wrappedStream.Position != desiredPosition)
                {
                    wrappedStream.Seek(desiredPosition);
                }

                bufferBase = streamPosition;
                int toRead = (int)Math.Min(buffer.Length, length - streamPosition);
                int read = wrappedStream.Read(buffer, 0, toRead);
                bufferTop = bufferBase + read;

                return read > 0;
            }
        }
    }
}
