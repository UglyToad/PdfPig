namespace UglyToad.PdfPig.Images.Png
{
    using System.Diagnostics.CodeAnalysis;
    using Content;
    using Graphics.Colors;
    using UglyToad.PdfPig.Core;

    internal static class PngFromPdfImageFactory
    {
        private static bool TryGenerateSoftMask(IPdfImage image, [NotNullWhen(true)] out ReadOnlySpan<byte> bytes)
        {
            bytes = ReadOnlySpan<byte>.Empty;

            if (!image.TryGetBytesAsMemory(out var imageMemory))
            {
                return false;
            }

            try
            {
                bytes = ColorSpaceDetailsByteConverter.Convert(image.ColorSpaceDetails!,
                    imageMemory.Span,
                    image.BitsPerComponent,
                    image.WidthInSamples,
                    image.HeightInSamples);
                return IsCorrectlySized(image, bytes);
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
                    image.BitsPerComponent, image.WidthInSamples, image.HeightInSamples);

                var numberOfComponents = image.ColorSpaceDetails!.BaseNumberOfColorComponents;

                ReadOnlySpan<byte> softMask = null;
                bool isSoftMask = image.SoftMaskImage is not null && TryGenerateSoftMask(image.SoftMaskImage, out softMask);

                var builder = PngBuilder.Create(image.WidthInSamples, image.HeightInSamples, isSoftMask);

                if (!IsCorrectlySized(image, bytesPure))
                {
                    return false;
                }

                if (image.ColorSpaceDetails.BaseType == ColorSpace.DeviceCMYK || numberOfComponents == 4)
                {
                    int i = 0;
                    int sm = 0;
                    for (int col = 0; col < image.HeightInSamples; col++)
                    {
                        for (int row = 0; row < image.WidthInSamples; row++)
                        {
                            /*
                             * Where CMYK in 0..1
                             * R = 255 × (1-C) × (1-K)
                             * G = 255 × (1-M) × (1-K)
                             * B = 255 × (1-Y) × (1-K)
                             */

                            byte a = isSoftMask ? softMask[sm++] : byte.MaxValue;
                            double c = (bytesPure[i++] / 255d);
                            double m = (bytesPure[i++] / 255d);
                            double y = (bytesPure[i++] / 255d);
                            double k = (bytesPure[i++] / 255d);
                            var r = (byte)(255 * (1 - c) * (1 - k));
                            var g = (byte)(255 * (1 - m) * (1 - k));
                            var b = (byte)(255 * (1 - y) * (1 - k));

                            builder.SetPixel(new Pixel(r, g, b, a, false), row, col);
                        }
                    }
                }
                else if (numberOfComponents == 3)
                {
                    int i = 0;
                    int sm = 0;
                    for (int col = 0; col < image.HeightInSamples; col++)
                    {
                        for (int row = 0; row < image.WidthInSamples; row++)
                        {
                            byte a = isSoftMask ? softMask[sm++] : byte.MaxValue;
                            builder.SetPixel(new Pixel(bytesPure[i++], bytesPure[i++], bytesPure[i++], a, false), row, col);
                        }
                    }
                }
                else
                {
                    int i = 0;
                    for (int col = 0; col < image.HeightInSamples; col++)
                    {
                        for (int row = 0; row < image.WidthInSamples; row++)
                        {
                            byte a = isSoftMask ? softMask[i] : byte.MaxValue;
                            byte pixel = bytesPure[i++];
                            builder.SetPixel(new Pixel(pixel, pixel, pixel, a, false), row, col);
                        }
                    }
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
