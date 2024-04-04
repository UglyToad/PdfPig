namespace UglyToad.PdfPig.Images.Png
{
    using System;
    using System.Buffers.Binary;
    using System.IO;

    internal static class StreamHelper
    {
        public static void WriteBigEndianInt32(Stream stream, int value)
        {
            Span<byte> buffer = stackalloc byte[4];

            BinaryPrimitives.WriteInt32BigEndian(buffer, value);

            stream.Write(buffer);
        }

        public static bool TryReadHeaderBytes(Stream stream, out byte[] bytes)
        {
            bytes = new byte[8];
            return stream.Read(bytes, 0, 8) == 8;
        }
    }
}