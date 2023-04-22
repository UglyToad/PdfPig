namespace UglyToad.PdfPig.Images.Png
{
    using Content;
    using Graphics.Colors;
    using UglyToad.PdfPig.Core;

    internal static class PngFromPdfImageFactory
    {
        public static bool TryGenerate(IPdfImage image, out byte[] bytes)
        {
            bytes = null;

            var hasValidDetails = image.ColorSpaceDetails != null && !(image.ColorSpaceDetails is UnsupportedColorSpaceDetails);

            var isColorSpaceSupported = hasValidDetails && image.ColorSpaceDetails.BaseType != ColorSpace.Pattern;

            if (!isColorSpaceSupported || !image.TryGetBytes(out var bytesPure))
            {
                return false;
            }

            try
            {
                bytesPure = ColorSpaceDetailsByteConverter.Convert(image.ColorSpaceDetails, bytesPure,
                    image.BitsPerComponent, image.WidthInSamples, image.HeightInSamples);

                var numberOfComponents = image.ColorSpaceDetails.BaseType == ColorSpace.ICCBased ? 3 : image.ColorSpaceDetails.BaseNumberOfColorComponents;

                var is3Byte = numberOfComponents == 3;

                var builder = PngBuilder.Create(image.WidthInSamples, image.HeightInSamples, false);

                var requiredSize = (image.WidthInSamples * image.HeightInSamples * numberOfComponents);

                var actualSize = bytesPure.Count;
                var isCorrectlySized = bytesPure.Count == requiredSize ||
                    // Spec, p. 37: "...error if the stream contains too much data, with the exception that
                    // there may be an extra end-of-line marker..."
                    (actualSize == requiredSize + 1 && bytesPure[actualSize - 1] == ReadHelper.AsciiLineFeed) ||
                    (actualSize == requiredSize + 1 && bytesPure[actualSize - 1] == ReadHelper.AsciiCarriageReturn) ||
                    // The combination of a CARRIAGE RETURN followed immediately by a LINE FEED is treated as one EOL marker.
                    (actualSize == requiredSize + 2 &&
                        bytesPure[actualSize - 2] == ReadHelper.AsciiCarriageReturn &&
                        bytesPure[actualSize - 1] == ReadHelper.AsciiLineFeed);

                if (!isCorrectlySized)
                {
                    return false;
                }

                if (image.ColorSpaceDetails.BaseType == ColorSpace.DeviceCMYK || numberOfComponents == 4)
                {
                    int i = 0;
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

                            double c = (bytesPure[i++] / 255d);
                            double m = (bytesPure[i++] / 255d);
                            double y = (bytesPure[i++] / 255d);
                            double k = (bytesPure[i++] / 255d);
                            var r = (byte)(255 * (1 - c) * (1 - k));
                            var g = (byte)(255 * (1 - m) * (1 - k));
                            var b = (byte)(255 * (1 - y) * (1 - k));

                            builder.SetPixel(r, g, b, row, col);
                        }
                    }
                }
                else if (is3Byte)
                {
                    int i = 0;
                    for (int col = 0; col < image.HeightInSamples; col++)
                    {
                        for (int row = 0; row < image.WidthInSamples; row++)
                        {
                            builder.SetPixel(bytesPure[i++], bytesPure[i++], bytesPure[i++], row, col);
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
                            byte pixel = bytesPure[i++];
                            builder.SetPixel(pixel, pixel, pixel, row, col);
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
