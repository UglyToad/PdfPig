namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using System;
    using IO;

    internal class ThrowingReader : IRandomAccessRead
    {
        private readonly IRandomAccessRead reader;

        public bool Throw { get; set; }

        public ThrowingReader(IRandomAccessRead reader)
        {
            this.reader = reader;
        }

        public void Dispose()
        {
            if (Throw) throw new InvalidOperationException();

            reader.Dispose();
        }

        public int Read()
        {
            if (Throw) throw new InvalidOperationException();
            return reader.Read();
        }

        public int Read(byte[] b)
        {
            if (Throw) throw new InvalidOperationException();
            return reader.Read(b);
        }

        public int Read(byte[] b, int offset, int length)
        {
            if (Throw) throw new InvalidOperationException();
            return reader.Read(b, offset, length);
        }

        public long GetPosition()
        {
            if (Throw) throw new InvalidOperationException();
            return reader.GetPosition();
        }

        public void Seek(long position)
        {
            if (Throw) throw new InvalidOperationException();
            reader.Seek(position);
        }

        public long Length()
        {
            if (Throw) throw new InvalidOperationException();
            return reader.Length();
        }

        public bool IsClosed()
        {
            if (Throw) throw new InvalidOperationException();
            return reader.IsClosed();
        }

        public int Peek()
        {
            if (Throw) throw new InvalidOperationException();
            return reader.Peek();
        }

        public void Rewind(int bytes)
        {
            if (Throw) throw new InvalidOperationException();
            reader.Rewind(bytes);
        }

        public byte[] ReadFully(int length)
        {
            if (Throw) throw new InvalidOperationException();
            return reader.ReadFully(length);
        }

        public bool IsEof()
        {
            if (Throw) throw new InvalidOperationException();
            return reader.IsEof();
        }

        public int Available()
        {
            if (Throw) throw new InvalidOperationException();
            return reader.Available();
        }

        public void ReturnToBeginning()
        {
            if (Throw) throw new InvalidOperationException();
            reader.ReturnToBeginning();
        }

        public void Unread(int b)
        {
            if (Throw) throw new InvalidOperationException();
            reader.Unread(b);
        }

        public void Unread(byte[] bytes)
        {
            if (Throw) throw new InvalidOperationException();
            reader.Unread(bytes);
        }

        public void Unread(byte[] bytes, int start, int length)
        {
            if (Throw) throw new InvalidOperationException();
            reader.Unread(bytes, start, length);
        }
    }
}