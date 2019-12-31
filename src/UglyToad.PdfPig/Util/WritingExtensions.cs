namespace UglyToad.PdfPig.Util
{
    using System.IO;

    internal static class WritingExtensions
    {
        public static void WriteUInt(this Stream stream, long value) => WriteUInt(stream, (uint) value);
        public static void WriteUInt(this Stream stream, uint value)
        {
            var buffer = new[]
                {
                    (byte) (value >> 24),
                    (byte) (value >> 16),
                    (byte) (value >> 8),
                    (byte) value
                };

            stream.Write(buffer, 0, 4);
        }

        public static void WriteUShort(this Stream stream, int value) => WriteUShort(stream, (ushort) value);
        public static void WriteUShort(this Stream stream, ushort value)
        {
            var buffer = new[]
            {
                    (byte) (value >> 8),
                    (byte) value
                };

            stream.Write(buffer, 0, 2);
        }

        public static void WriteShort(this Stream stream, ushort value) => WriteShort(stream, (short)value);
        public static void WriteShort(this Stream stream, short value)
        {
            var buffer = new[]
            {
                (byte) (value >> 8),
                (byte) value
            };

            stream.Write(buffer, 0, 2);
        }
    }
}
