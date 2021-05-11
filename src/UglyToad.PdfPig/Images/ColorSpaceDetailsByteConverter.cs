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
            var bytesPerPixel = details is IndexedColorSpaceDetails ? 1 : GetBytesPerPixel(details);
            var strideWidth = decoded.Count / imageHeight / bytesPerPixel;
            if (strideWidth != imageWidth)
            {
                decoded = RemoveStridePadding(decoded.ToArray(), strideWidth, imageWidth, imageHeight, bytesPerPixel);
            }

            // In case of indexed color space images, unwrap indices to actual pixel component values
            if (details is IndexedColorSpaceDetails indexed)
            {
                decoded = UnwrapIndexedColorSpaceBytes(indexed, decoded);
            }

            return decoded.ToArray();
        }

        private static int GetBytesPerPixel(ColorSpaceDetails details)
        {
            var colorSpace = (details is IndexedColorSpaceDetails indexed) ? indexed.BaseColorSpaceDetails.Type : details.Type;
            switch (colorSpace)
            {
                case ColorSpace.DeviceRGB:
                    return 3;

                case ColorSpace.DeviceCMYK:
                    return 4;

                default:
                    return 1;
            }
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

            return input.SelectMany(b => Unpack(b)).ToArray();
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

        private static byte[] UnwrapIndexedColorSpaceBytes(IndexedColorSpaceDetails indexed, IReadOnlyList<byte> input)
        {
            var multiplier = 1;
            Func<byte, IEnumerable<byte>> transformer = null;
            switch (indexed.BaseColorSpaceDetails.Type)
            {
                case ColorSpace.DeviceRGB:
                    transformer = x =>
                    {
                        var r = new byte[3];
                        for (var i = 0; i < 3; i++)
                        {
                            r[i] = indexed.ColorTable[x * 3 + i];
                        }

                        return r;
                    };
                    multiplier = 3;
                    break;
                case ColorSpace.DeviceCMYK:
                    transformer = x =>
                    {
                        var r = new byte[4];
                        for (var i = 0; i < 4; i++)
                        {
                            r[i] = indexed.ColorTable[x * 4 + i];
                        }

                        return r;
                    };

                    multiplier = 4;
                    break;
                case ColorSpace.DeviceGray:
                    transformer = x => new[] { indexed.ColorTable[x] };
                    multiplier = 1;
                    break;
            }

            if (transformer != null)
            {
                var result = new byte[input.Count * multiplier];
                var i = 0;
                foreach (var b in input)
                {
                    foreach (var newByte in transformer(b))
                    {
                        result[i++] = newByte;
                    }
                }

                return result;
            }

            return input.ToArray();
        }
    }
}
