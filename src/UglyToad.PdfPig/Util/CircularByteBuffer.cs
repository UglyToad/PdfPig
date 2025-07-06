namespace UglyToad.PdfPig.Util;

using System.Text;

internal class CircularByteBuffer(int size)
{
        private readonly byte[] buffer = new byte[size];

        private int start;
        private int count;

        public void Add(byte b)
        {
            var insertionPosition = (start + count) % buffer.Length;

            buffer[insertionPosition] = b;
            if (count < buffer.Length)
            {
                count++;
            }
            else
            {
                start = (start + 1) % buffer.Length;
            }
        }

        public bool EndsWith(string s)
        {
            if (s.Length > count)
            {
                return false;
            }

            for (var i = 0; i < s.Length; i++)
            {
                var str = s[i];

                var inBuffer = count - (s.Length - i);

                var buff = buffer[IndexToBufferIndex(inBuffer)];

                if (buff != str)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsCurrentlyEqual(string s)
        {
            if (s.Length > buffer.Length)
            {
                return false;
            }

            for (var i = 0; i < s.Length; i++)
            {
                var b = (byte)s[i];
                var buff = buffer[IndexToBufferIndex(i)];

                if (b != buff)
                {
                    return false;
                }
            }

            return true;
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            Span<byte> tmp = new byte[count];
            for (int i = 0; i < count; i++)
            {
                tmp[i] = buffer[IndexToBufferIndex(i)];
            }

            return tmp;
        }

        public override string ToString()
        {
            return Encoding.ASCII.GetString(AsSpan());
        }

        private int IndexToBufferIndex(int i) => (start + i) % buffer.Length;
}
