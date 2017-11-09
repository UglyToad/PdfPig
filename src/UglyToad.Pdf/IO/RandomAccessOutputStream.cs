namespace UglyToad.Pdf.IO
{
    public class RandomAccessOutputStream : IOutputStream
    {
        private readonly RandomAccessWrite writer;

        /**
         * Constructor to create a new output stream which writes to the given RandomAccessWrite.
         *
         * @param writer The random access writer for output
         */
        public RandomAccessOutputStream(RandomAccessWrite writer)
        {
            this.writer = writer;
            // we don't have to maintain a position, as each COSStream can only have one writer.
        }

        public void write(byte[] b, int offset, int length)
        {
            writer.write(b, offset, length);
        }

        public void flush()
        {
        }

        public void write(byte[] b)
        {
            writer.write(b);
        }

        public void write(int b)
        {
            writer.write(b);
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }
}