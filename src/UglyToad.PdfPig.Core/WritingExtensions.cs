namespace UglyToad.PdfPig.Core
{
    using System.IO;

    /// <summary>
    /// Handles writing specified data types to an output stream in a valid PDF compliant format.
    /// </summary>
    public static class WritingExtensions
    {
        /// <summary>
        /// Write the <see langword="long"/> to the stream as a <see langword="uint"/>.
        /// </summary>
        public static void WriteUInt(this Stream stream, long value) => WriteUInt(stream, (uint)value);
        /// <summary>
        /// Write the <see langword="uint"/> to the stream as a <see langword="uint"/>.
        /// </summary>
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

        /// <summary>
        /// Write the <see langword="int"/> to the stream as a <see langword="ushort"/>.
        /// </summary>
        public static void WriteUShort(this Stream stream, int value) => WriteUShort(stream, (ushort)value);
        /// <summary>
        /// Write the <see langword="ushort"/> to the stream as a <see langword="ushort"/>.
        /// </summary>
        public static void WriteUShort(this Stream stream, ushort value)
        {
            var buffer = new[]
            {
                    (byte) (value >> 8),
                    (byte) value
                };

            stream.Write(buffer, 0, 2);
        }

        /// <summary>
        /// Write the <see langword="ushort"/> to the stream as a <see langword="short"/>.
        /// </summary>
        public static void WriteShort(this Stream stream, ushort value) => WriteShort(stream, (short)value);
        /// <summary>
        /// Write the <see langword="short"/> to the stream as a <see langword="short"/>.
        /// </summary>
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
