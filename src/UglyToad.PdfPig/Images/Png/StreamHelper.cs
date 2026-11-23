namespace UglyToad.PdfPig.Images.Png
{
    using System.IO;

    internal static class StreamHelper
    {
        public static bool TryReadHeaderBytes(Stream stream, out byte[] bytes)
        {
            bytes = new byte[8];
            return stream.Read(bytes, 0, 8) == 8;
        }
    }
}