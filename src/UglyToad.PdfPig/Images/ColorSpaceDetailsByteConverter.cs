namespace UglyToad.PdfPig.Images
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Core;
    using Graphics.Colors;

    /// <summary>
    /// Utility for working with the bytes in <see cref="IPdfImage"/>s and converting according to their <see cref="ColorSpaceDetails"/>.s
    /// </summary>
    public static class ColorSpaceDetailsByteConverter
    {
        /// <summary>
        /// Converts the output bytes (if available) of <see cref="IPdfImage.TryGetBytes"/>
        /// to actual pixel values using the <see cref="IPdfImage.ColorSpaceDetails"/>. For most images this doesn't
        /// change the data but for <see cref="ColorSpace.Indexed"/> it will convert the bytes which are indexes into the
        /// real pixel data into the real pixel data.
        /// </summary>
        public static byte[] Convert(ColorSpaceDetails details, IReadOnlyList<byte> decoded, int bitsPerComponent, int imageWidth, int imageHeight)
        {
            if (decoded == null)
            {
                return EmptyArray<byte>.Instance;
            }

            if (details == null)
            {
                return decoded.ToArray();
            }

            if (bitsPerComponent != 8)
            {
                // Unpack components such that they occupy one byte each
                decoded = UnpackComponents(decoded, bitsPerComponent);
            }

            // Remove padding bytes when the stride width differs from the image width
            var bytesPerPixel = details.NumberOfColorComponents;
            var strideWidth = decoded.Count / imageHeight / bytesPerPixel;
            if (strideWidth != imageWidth)
            {
                decoded = RemoveStridePadding(decoded.ToArray(), strideWidth, imageWidth, imageHeight, bytesPerPixel);
            }

            decoded = details.Transform(decoded);

            return decoded.ToArray();
        }

        private static byte[] UnpackComponents(IReadOnlyList<byte> input, int bitsPerComponent)
        {
            IEnumerable<byte> Unpack(byte b)
            {
                // Enumerate bits in bitsPerComponent-sized chunks from MSB to LSB, masking on the appropriate bits
                for (int i = 8 - bitsPerComponent; i >= 0; i -= bitsPerComponent)
                {
                    yield return (byte)((b >> i) & ((int)Math.Pow(2, bitsPerComponent) - 1));
                }
            }

            return input.SelectMany(Unpack).ToArray();
        }

        private static byte[] RemoveStridePadding(byte[] input, int strideWidth, int imageWidth, int imageHeight, int multiplier)
        {
            var result = new byte[imageWidth * imageHeight * multiplier];
            for (int y = 0; y < imageHeight; y++)
            {
                int sourceIndex = y * strideWidth;
                int targetIndex = y * imageWidth;
                Array.Copy(input, sourceIndex, result, targetIndex, imageWidth);
            }

            return result;
        }
    }
}
