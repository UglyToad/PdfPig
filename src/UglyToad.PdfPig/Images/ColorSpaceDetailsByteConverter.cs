namespace UglyToad.PdfPig.Images
{
    using Content;
    using Graphics.Colors;
    using System;
    using System.Collections.Generic;
    using UglyToad.PdfPig.Graphics.Core;

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
            return Convert(details, decoded, bitsPerComponent, imageWidth, imageHeight, null, RenderingIntent.RelativeColorimetric);
        }

        /// <summary>
        /// Converts the output bytes (if available) of <see cref="IPdfImage.TryGetBytesAsMemory"/>
        /// to actual pixel values using the <see cref="IPdfImage.ColorSpaceDetails"/>, applying the image's
        /// <paramref name="decode"/> array before the colour space transform.
        /// </summary>
        /// <remarks>
        /// The Decode array (PDF 2.0, 8.9.5.10) maps each raw sample value to a value in the colour-space range
        /// before the colour space transform is applied. An empty or null <paramref name="decode"/> means "use the
        /// colour space default" (which is a no-op for the common case). For <see cref="ColorSpace.Indexed"/> the
        /// sample is itself an index so the byte-level Decode applied here is skipped.
        /// </remarks>
        public static Span<byte> Convert(ColorSpaceDetails details, Span<byte> decoded, int bitsPerComponent, int imageWidth, int imageHeight,
            IReadOnlyList<double>? decode, RenderingIntent intent)
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
                decoded = UnpackComponents(decoded, bitsPerComponent);
            }

            // Remove padding bytes when the stride width differs from the image width
            var bytesPerPixel = details.NumberOfColorComponents;
            var strideWidth = decoded.Length / imageHeight / bytesPerPixel;
            if (strideWidth != imageWidth)
            {
                if (bytesPerPixel > 1 && imageWidth * imageHeight * bytesPerPixel < decoded.Length)
                {
                    // Fixed thanks to / see discussion at https://github.com/UglyToad/PdfPig/issues/1183
                    // Unclear what should be done here, we assume we can just remove the trailing bytes
                    decoded = decoded.Slice(0, imageWidth * imageHeight * bytesPerPixel);
                }
                else
                {
                    decoded = RemoveStridePadding(decoded, strideWidth, imageWidth, imageHeight, bytesPerPixel);
                }
            }

            ApplyDecode(decoded, details, decode, bitsPerComponent);

            return details.Transform(decoded, intent);
        }

        private static void ApplyDecode(Span<byte> samples, ColorSpaceDetails details, IReadOnlyList<double>? decode, int bitsPerComponent)
        {
            if (bitsPerComponent == 16)
            {
                // bpc passed to ApplyDecode reflects the *post-unpack* sample range:
                //   - 16bpc is reduced to its 8-bit high-byte equivalent inside UnpackComponents.
                //   - sub-8bpc samples remain in [0, 2^bpc - 1] after unpacking.
                bitsPerComponent = 8;
            }

            int components = details.NumberOfColorComponents;
            int sampleMax = (1 << bitsPerComponent) - 1;
            if (sampleMax <= 0)
            {
                return;
            }

            Span<double> defaults = components <= 16 ? stackalloc double[2 * components] : new double[2 * components];
            details.GetDefaultDecode(bitsPerComponent, defaults);

            bool isIndexed = details.Type == ColorSpace.Indexed;
            bool hasDecode = decode is not null && decode.Count >= components * 2;

            // Fast path: the byte would come out as it went in.
            //   - Indexed: the default Decode is the identity at any bit depth (S -> S).
            //   - Non-Indexed 8bpc: the sample already IS the position within the default range, which is
            //     exactly what a byte encodes (see below), so the round trip cancels.
            // Sub-8bpc still has to be stretched from [0, 2^bpc - 1] to [0, 255].
            if (!hasDecode || IsDefaultDecode(decode!, components, defaults))
            {
                if (isIndexed || bitsPerComponent == 8)
                {
                    return;
                }
            }

            // Per PDF 2.0 8.9.5.10, for each component c with raw sample S in [0, sampleMax]:
            //     x_c = Dmin_c + S * (Dmax_c - Dmin_c) / sampleMax
            //
            // What the byte then holds differs by colour space:
            //   - Indexed: x_c is the palette index itself, kept as-is in [0, sampleMax].
            //   - Non-Indexed: a byte cannot hold an L* of 100 or a negative a*, so it holds x_c's
            //     POSITION within the component's default range, scaled to [0, 255]. The colour space
            //     reverses this in its Transform via DecodeRawComponents, so the pair round-trips. For the
            //     usual [0, 1] space position and value coincide and this is the historical behaviour.
            for (int i = 0; i < samples.Length; i++)
            {
                int c = i % components;
                double defaultMin = defaults[c * 2];
                double defaultMax = defaults[c * 2 + 1];

                double dMin = hasDecode ? decode![c * 2] : defaultMin;
                double dMax = hasDecode ? decode![c * 2 + 1] : defaultMax;
                double x = dMin + samples[i] * (dMax - dMin) / sampleMax;

                double outputScale;
                int outputMax;
                if (isIndexed)
                {
                    outputScale = 1.0;
                    outputMax = sampleMax;
                }
                else
                {
                    double span = defaultMax - defaultMin;

                    // A degenerate range (Lab /Range [0 0 0 0] is legal) pins every sample to the single
                    // valid value, which is position zero.
                    x = span == 0.0 ? 0.0 : (x - defaultMin) / span;
                    outputScale = 255.0;
                    outputMax = 255;
                }

                int rounded = (int)Math.Round(x * outputScale, MidpointRounding.AwayFromZero);
                if (rounded < 0)
                {
                    rounded = 0;
                }
                else if (rounded > outputMax)
                {
                    rounded = outputMax;
                }

                samples[i] = (byte)rounded;
            }
        }

        private static bool IsDefaultDecode(IReadOnlyList<double> decode, int components, ReadOnlySpan<double> defaults)
        {
            for (int c = 0; c < components; c++)
            {
                if (decode[c * 2] != defaults[c * 2] || decode[c * 2 + 1] != defaults[c * 2 + 1])
                {
                    return false;
                }
            }

            return true;
        }

        private static Span<byte> UnpackComponents(Span<byte> input, int bitsPerComponent)
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
            foreach (byte b in input)
            {
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
