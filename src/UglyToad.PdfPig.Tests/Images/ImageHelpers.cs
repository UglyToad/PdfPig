namespace UglyToad.PdfPig.Tests.Images
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using UglyToad.PdfPig.Images.Png;

    public static class ImageHelpers
    {
        private static readonly string FilesFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Images", "Files"));

        public static byte[] LoadFileBytes(string filename, bool isCompressed = false)
        {
            var filePath = Path.Combine(FilesFolder, filename);
            var memoryStream = new MemoryStream();
            if (isCompressed)
            {
                using (var deflateStream = new DeflateStream(File.OpenRead(filePath), CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(memoryStream);
                }

                return memoryStream.ToArray();
            }

            return File.ReadAllBytes(filePath);
        }

        public static bool ImagesAreEqual(byte[] first, byte[] second)
        {
            var png1 = Png.Open(first);
            var png2 = Png.Open(second);
            if (png1.Width != png2.Width || png1.Height != png2.Height || png1.HasAlphaChannel != png2.HasAlphaChannel)
            {
                return false;
            }

            for (var y = 0; y < png1.Height; y++)
            {
                for (var x = 0; x < png1.Width; x++)
                {
                    if (!png1.GetPixel(x, y).Equals(png2.GetPixel(x, y))) 
                    { 
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
