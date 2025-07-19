namespace UglyToad.PdfPig.Images
{
    using Content;
    using Graphics.Colors;
    using System;

    /// <summary>
    /// Utility for working with the bytes in <see cref="IPdfImage"/>s and converting according to their <see cref="ColorSpaceDetails"/>.s
    /// </summary>
    public static class ColorSpaceDetailsByteConverter
    {
        /// <summary>
        /// Converts the output bytes (if available) of <see cref="IPdfImage.TryGetBytesAsMemory"/>
        /// to actual pixel values using the <see cref="IPdfImage.ColorSpaceDetails"/>. For most images this doesn't
        /// change the data but for <see cref="ColorSpace.Indexed"/> it will convert the bytes which are indexes into the
        /// real pixel data into the real pixel data.
        /// </summary>
        public static Span<byte> Convert(ColorSpaceDetails details, Span<byte> decoded, int bitsPerComponent, int imageWidth, int imageHeight)
        {
            if (decoded.IsEmpty)
            {
                return [];
            }

            if (details is null)
            {
                return decoded;
            }


            if (bitsPerComponent != 8)
            {
                // Unpack components such that they occupy one byte each
                decoded = UnpackComponents(decoded, bitsPerComponent, details.Type);
            }

            // Remove padding bytes when the stride width differs from the image width
            var bytesPerPixel = details.NumberOfColorComponents;
            var strideWidth = decoded.Length / imageHeight / bytesPerPixel;
            if (strideWidth != imageWidth)
            {
                decoded = RemoveStridePadding(decoded, strideWidth, imageWidth, imageHeight, bytesPerPixel);
            }

            return details.Transform(decoded);
        }

        private static Span<byte> UnpackComponents(Span<byte> input, int bitsPerComponent, ColorSpace colorSpace)
        {
            if (bitsPerComponent == 16) // Example with MOZILLA-3136-0.pdf (page 3)
            {
                int size = input.Length / 2;
                var unpacked16 = input.Slice(0, size); // In place

                for (int b = 0; b < size; ++b)
                {
                    int i = 2 * b;
                    // Convert to UInt16 and divide by 256
                    unpacked16[b] = (byte)((ushort)(input[i + 1] | input[i] << 8) / 256);
                }

                return unpacked16;
            }

            int end = 8 - bitsPerComponent;

            Span<byte> unpacked = new byte[input.Length * (int)Math.Ceiling((end + 1) / (double)bitsPerComponent)];

            int right = (int)Math.Pow(2, bitsPerComponent) - 1;

            int u = 0;

            // TODO - 1bpc + DeviceGray is required for JBIG2 but needs to be investigated
            // Why is this required? This does not belong here imo
            if (bitsPerComponent == 1 && colorSpace != ColorSpace.Indexed)
            {
                foreach (byte b in input)
                {
                    // Enumerate bits in bitsPerComponent-sized chunks from MSB to LSB, masking on the appropriate bits
                    for (int i = end; i >= 0; --i)
                    {
                        unpacked[u++] = (byte)((b >> i) & right) == 1 ? byte.MaxValue : byte.MinValue;
                    }
                }

                return unpacked;
            }

            // Default method
            foreach (byte b in input)
            {
                // Enumerate bits in bitsPerComponent-sized chunks from MSB to LSB, masking on the appropriate bits
                for (int i = end; i >= 0; i -= bitsPerComponent)
                {
                    unpacked[u++] = (byte)((b >> i) & right);
                }
            }

            return unpacked;
        }

        private static Span<byte> RemoveStridePadding(Span<byte> input, int strideWidth, int imageWidth, int imageHeight, int multiplier)
        {
            int size = imageWidth * imageHeight * multiplier;
            Span<byte> result = size < input.Length ? input.Slice(0, size) : new byte[size];
            // See PDFBOX-492-4.jar-8.pdf, page 2 for size > input.Length

            for (int y = 0; y < imageHeight; y++)
            {
                int sourceIndex = y * strideWidth;
                int targetIndex = y * imageWidth;
                input.Slice(sourceIndex, imageWidth).CopyTo(result.Slice(targetIndex, imageWidth));
            }

            return result;
        }
    }
}
