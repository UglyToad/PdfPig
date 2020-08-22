namespace UglyToad.PdfPig.Images.Png
{
    using System;

    /// <summary>
    /// Provides convenience methods for indexing into a raw byte array to extract pixel values.
    /// </summary>
    internal class RawPngData
    {
        private readonly byte[] data;
        private readonly int bytesPerPixel;
        private readonly int width;
        private readonly Palette palette;
        private readonly ColorType colorType;
        private readonly int rowOffset;

        /// <summary>
        /// Create a new <see cref="RawPngData"/>.
        /// </summary>
        /// <param name="data">The decoded pixel data as bytes.</param>
        /// <param name="bytesPerPixel">The number of bytes in each pixel.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="interlaceMethod">The interlace method used.</param>
        /// <param name="palette">The palette for images using indexed colors.</param>
        /// <param name="colorType">The color type.</param>
        public RawPngData(byte[] data, int bytesPerPixel, int width, InterlaceMethod interlaceMethod, Palette palette, ColorType colorType)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException($"Width must be greater than or equal to 0, got {width}.");
            }

            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.bytesPerPixel = bytesPerPixel;
            this.width = width;
            this.palette = palette;
            this.colorType = colorType;
            rowOffset = interlaceMethod == InterlaceMethod.Adam7 ? 0 : 1;
        }

        public Pixel GetPixel(int x, int y)
        {
            var rowStartPixel = (rowOffset + (rowOffset * y)) + (bytesPerPixel * width * y);

            var pixelStartIndex = rowStartPixel + (bytesPerPixel * x);

            var first = data[pixelStartIndex];

            if (palette != null)
            {
                return palette.GetPixel(first);
            }

            switch (bytesPerPixel)
            {
                case 1:
                    return new Pixel(first, first, first, 255, true);
                case 2:
                    switch (colorType)
                    {
                        case ColorType.None:
                        {
                            byte second = data[pixelStartIndex + 1];
                            var value = ToSingleByte(first, second);
                            return new Pixel(value, value, value, 255, true);

                        }
                        default:
                            return new Pixel(first, first, first, data[pixelStartIndex + 1], true);
                    }

                case 3:
                    return new Pixel(first, data[pixelStartIndex + 1], data[pixelStartIndex + 2], 255, false);
                case 4:
                    switch (colorType)
                    {
                        case ColorType.None | ColorType.AlphaChannelUsed:
                        {
                            var second = data[pixelStartIndex + 1];
                            var firstAlpha = data[pixelStartIndex + 2];
                            var secondAlpha = data[pixelStartIndex + 3];
                            var gray = ToSingleByte(first, second);
                            var alpha = ToSingleByte(firstAlpha, secondAlpha);
                            return new Pixel(gray, gray, gray, alpha, true);
                        }
                        default:
                            return new Pixel(first, data[pixelStartIndex + 1], data[pixelStartIndex + 2], data[pixelStartIndex + 3], false);
                    }
                case 6:
                    return new Pixel(first, data[pixelStartIndex + 2], data[pixelStartIndex + 4], 255, false);
                case 8:
                    return new Pixel(first, data[pixelStartIndex + 2], data[pixelStartIndex + 4], data[pixelStartIndex + 6], false);
                default:
                    throw new InvalidOperationException($"Unreconized number of bytes per pixel: {bytesPerPixel}.");
            }
        }

        private static byte ToSingleByte(byte first, byte second)
        {
            var us = (first << 8) + second;
            var result = (byte)Math.Round((255 * us) / (double)ushort.MaxValue);
            return result;
        }
    }
}