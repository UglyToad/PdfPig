namespace UglyToad.Pdf.IO
{
    public class RandomAccessInputStream : IInputStream
    {
        private readonly IRandomAccessRead input;
        private long position;

        /**
         * Creates a new RandomAccessInputStream, with a position of zero. The InputStream will maintain
         * its own position independent of the RandomAccessRead.
         *
         * @param randomAccessRead The RandomAccessRead to read from.
         */
        public RandomAccessInputStream(IRandomAccessRead randomAccessRead)
        {
            input = randomAccessRead;
            position = 0;
        }

        void restorePosition()
        {
            input.Seek(position);
        }

        public long available()
        {
            restorePosition();
            long available = input.Length() - input.GetPosition();
            if (available > int.MaxValue)
            {
                return int.MaxValue;
            }
            return available;
        }

        public int read()
        {
            restorePosition();
            if (input.IsEof())
            {
                return -1;
            }
            int b = input.Read();
            position += 1;
            return b;
        }

        public int read(byte[] b)
        {
            return read(b, 0, b.Length);
        }

        public int read(byte[] b, int off, int len)
        {
            restorePosition();
            if (input.IsEof())
            {
                return -1;
            }
            int n = input.Read(b, off, len);
            position += n;
            return n;
        }

        public long skip(long n)
        {
            restorePosition();
            input.Seek(position + n);
            position += n;
            return n;
        }

        public void Dispose()
        {
            input.Dispose();
        }

        long IInputStream.available()
        {
            throw new System.NotImplementedException();
        }
    }
}