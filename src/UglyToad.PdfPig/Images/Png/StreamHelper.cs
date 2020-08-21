namespace UglyToad.PdfPig.Images.Png
{
    using System.IO;

    internal static class StreamHelper
    {
        public static int ReadBigEndianInt32(byte[] bytes, int offset)
        {
            return (bytes[0 + offset] << 24) + (bytes[1 + offset] << 16)
                                             + (bytes[2 + offset] << 8) + bytes[3 + offset];
        }

        public static bool TryReadHeaderBytes(Stream stream, out byte[] bytes)
        {
            bytes = new byte[8];
            return stream.Read(bytes, 0, 8) == 8;
        }
    }
}