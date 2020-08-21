namespace UglyToad.PdfPig.Images.Png
{
    internal class Palette
    {
        public byte[] Data { get; }

        public Palette(byte[] data)
        {
            Data = data;
        }

        public Pixel GetPixel(int index)
        {
            var start = index * 3;

            return new Pixel(Data[start], Data[start + 1], Data[start + 2], 255, false);
        }
    }
}