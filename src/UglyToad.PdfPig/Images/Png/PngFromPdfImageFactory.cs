namespace UglyToad.PdfPig.Images.Png
{
    using System.Diagnostics.CodeAnalysis;
    using Content;
    using Graphics.Colors;
    using Core;

    internal static class PngFromPdfImageFactory
    {
        private static bool TryGenerateSoftMask(IPdfImage image, [NotNullWhen(true)] out ReadOnlySpan<byte> maskBytes)
        {
            maskBytes = ReadOnlySpan<byte>.Empty;

            var mask = image.MaskImage;

            if (mask is null)
            {
                return false;
            }

            // Because we cannot resize images directly in PdfPig, we only
            // apply the mask if it has the same size as the image
            if (image.HeightInSamples != mask.HeightInSamples ||
                image.WidthInSamples != mask.WidthInSamples)
            {
                return false;
            }

            if (!mask.TryGetBytesAsMemory(out var maskMemory))
            {
                return false;
            }

            try
            {
                maskBytes = ColorSpaceDetailsByteConverter.Convert(
                    mask.ColorSpaceDetails!,
                    maskMemory.Span,
                    mask.BitsPerComponent,
                    mask.WidthInSamples,
                    mask.HeightInSamples);

                return IsCorrectlySized(mask, maskBytes);
            }
            catch (Exception)
            {
                // ignored.
            }

            return false;
        }

        private static bool IsCorrectlySized(IPdfImage image, ReadOnlySpan<byte> bytesPure)
        {
            var numberOfComponents = image.ColorSpaceDetails!.BaseNumberOfColorComponents;
            var requiredSize = (image.WidthInSamples * image.HeightInSamples * numberOfComponents);

            var actualSize = bytesPure.Length;

            return bytesPure.Length == requiredSize ||
                // Spec, p. 37: "...error if the stream contains too much data, with the exception that
                // there may be an extra end-of-line marker..."
                (actualSize == requiredSize + 1 && bytesPure[actualSize - 1] == ReadHelper.AsciiLineFeed) ||
                (actualSize == requiredSize + 1 && bytesPure[actualSize - 1] == ReadHelper.AsciiCarriageReturn) ||
                // The combination of a CARRIAGE RETURN followed immediately by a LINE FEED is treated as one EOL marker.
                (actualSize == requiredSize + 2 &&
                    bytesPure[actualSize - 2] == ReadHelper.AsciiCarriageReturn &&
                    bytesPure[actualSize - 1] == ReadHelper.AsciiLineFeed);
        }

        public static bool TryGenerate(IPdfImage image, [NotNullWhen(true)] out byte[]? bytes)
        {
            bytes = null;

            var hasValidDetails = image.ColorSpaceDetails != null && !(image.ColorSpaceDetails is UnsupportedColorSpaceDetails);

            var isColorSpaceSupported = hasValidDetails && image.ColorSpaceDetails!.BaseType != ColorSpace.Pattern;

            if (!isColorSpaceSupported || !image.TryGetBytesAsMemory(out var imageMemory))
            {
                return false;
            }

            var bytesPure = imageMemory.Span;

            try
            {
                bytesPure = ColorSpaceDetailsByteConverter.Convert(image.ColorSpaceDetails!, bytesPure,
                    image.BitsPerComponent, image.WidthInSamples, image.HeightInSamples, image.Decode, image.RenderingIntent);

                if (!IsCorrectlySized(image, bytesPure))
                {
                    return false;
                }

                var numberOfComponents = image.ColorSpaceDetails!.BaseNumberOfColorComponents;
                var isCmyk = image.ColorSpaceDetails.BaseType == ColorSpace.DeviceCMYK || numberOfComponents == 4;
                var isRgb = !isCmyk && numberOfComponents == 3;

                bool hasMask = TryGenerateSoftMask(image, out var softMask);
                bool reverseMask = hasMask && image.MaskImage!.NeedsReverseDecode();

                var width = image.WidthInSamples;
                var height = image.HeightInSamples;

                using var builder = PngBuilder.Create(width, height, hasMask);

                var bytesPerPixel = hasMask ? 4 : 3;

                int i = 0;
                int sm = 0;
                for (int y = 0; y < height; y++)
                {
                    var row = builder.NextRow();

                    if (isCmyk)
                    {
                        for (int o = 0; o < row.Length; o += bytesPerPixel)
                        {
                            /*
                             * Where CMYK in 0..1
                             * R = 255 × (1-C) × (1-K)
                             * G = 255 × (1-M) × (1-K)
                             * B = 255 × (1-Y) × (1-K)
                             */
                            double c = (bytesPure[i++] / 255d);
                            double m = (bytesPure[i++] / 255d);
                            double yellow = (bytesPure[i++] / 255d);
                            double k = (bytesPure[i++] / 255d);
                            row[o] = (byte)(255 * (1 - c) * (1 - k));
                            row[o + 1] = (byte)(255 * (1 - m) * (1 - k));
                            row[o + 2] = (byte)(255 * (1 - yellow) * (1 - k));
                        }
                    }
                    else if (isRgb && !hasMask)
                    {
                        // The samples are already laid out as a PNG row.
                        bytesPure.Slice(i, row.Length).CopyTo(row);
                        i += row.Length;
                    }
                    else if (isRgb)
                    {
                        for (int o = 0; o < row.Length; o += bytesPerPixel)
                        {
                            row[o] = bytesPure[i++];
                            row[o + 1] = bytesPure[i++];
                            row[o + 2] = bytesPure[i++];
                        }
                    }
                    else
                    {
                        for (int o = 0; o < row.Length; o += bytesPerPixel)
                        {
                            byte pixel = bytesPure[i++];
                            row[o] = pixel;
                            row[o + 1] = pixel;
                            row[o + 2] = pixel;
                        }
                    }

                    if (hasMask)
                    {
                        for (int o = 3; o < row.Length; o += 4)
                        {
                            row[o] = reverseMask ? (byte)(255 - softMask[sm]) : softMask[sm];
                            sm++;
                        }
                    }

                    builder.WriteRow();
                }

                bytes = builder.Save();
                return true;
            }
            catch
            {
                // ignored.
            }

            return false;
        }
    }
}
