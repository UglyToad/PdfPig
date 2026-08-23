namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System;
    using System.Collections.Generic;
    using Core;

    /// <summary>
    /// Colour-manages device colours through a document's output intent (14.11.5, "Output intents", and
    /// 8.6.5.7, "Implicit conversion of CIE-based colour spaces").
    /// <para>
    /// This is opt-in via <see cref="IIccProfileService.UseOutputIntent"/>. When it is off, device colours
    /// keep their built-in conversion.
    /// </para>
    /// </summary>
    public static class OutputIntentColorManagement
    {
        /// <summary>
        /// The embedded profile that characterises the output device for the content being processed, or
        /// <see langword="null"/> when nothing does.
        /// </summary>
        /// <param name="outputIntents">The output intents in effect, in array order.</param>
        /// <param name="service">Cannot be <see langword="null"/>. The configured service, whose
        /// <see cref="IIccProfileService.UseOutputIntent"/> decides whether to manage at all
        /// and whose <see cref="IIccProfileService.PreferredOutputIntentSubtype"/> breaks ties.</param>
        public static IIccProfile? GetDeviceProfile(IReadOnlyList<OutputIntent>? outputIntents, IIccProfileService service)
        {
            if (!service.UseOutputIntent)
            {
                return null;
            }

            return OutputIntent
                .SelectForColorManagement(outputIntents, service.PreferredOutputIntentSubtype)?
                .DestOutputProfile;
        }

        /// <summary>
        /// Convert a device colour through an output intent profile to RGB, returning <see langword="true"/>
        /// and the managed <see cref="RGBColor"/> when the colour is a device colour expressible in the profile's
        /// colour space.
        /// Returns <see langword="false"/> for non-device colours, for profile/device mismatches with no neutral
        /// mapping (e.g. DeviceRGB with a CMYK output intent), and when the transform cannot be built.
        /// </summary>
        public static bool TryConvert(IColor? color, ColorSpace colorSpaceType, IIccProfile profile,
            RenderingIntent intent, out IColor? managed)
        {
            managed = null;

            if (!TryGetDeviceComponents(color, colorSpaceType, out double[] values) ||
                !TryMapDeviceToProfileComponents(colorSpaceType, values, profile.NumberOfComponents, out var deviceValues) ||
                !profile.TryGetTransform(intent, out var transform) ||
                !transform.TryToRgbClipped(deviceValues, out double r, out double g, out double b))
            {
                return false;
            }

            managed = new RGBColor(r, g, b);
            return true;
        }

        /// <summary>
        /// The transform that colour-manages a raster image's samples through the output intent, or
        /// <see langword="null"/> when it should keep its built-in conversion. The counterpart of
        /// <see cref="TryConvert"/> for the image path, so that the two cannot disagree about which images
        /// and colour spaces are eligible; applying it to the samples is the consumer's job, since that is
        /// rasterisation.
        /// <para>
        /// The returned transform consumes exactly <see cref="ColorSpaceDetails.BaseNumberOfColorComponents"/>
        /// components per pixel - its <see cref="IIccTransform.NumberOfComponents"/> is the image's, not the
        /// profile's. Where the two differ the profile transform is wrapped so that the DeviceGray expansion
        /// happens here, beside the <see cref="TryMapDeviceToProfileComponents"/> that does the same job for a
        /// single colour, rather than in each consumer where the two could drift apart.
        /// </para>
        /// </summary>
        /// <param name="colorSpace">The image's colour space, or <see langword="null"/> if it has none.</param>
        /// <param name="imageRenderingIntent">The image's rendering intent.</param>
        /// <param name="profile">The output intent profile in effect, from
        /// <see cref="CurrentGraphicsState.OutputIntentProfile"/>; <see langword="null"/> when none is,
        /// which includes a soft-mask group, where device values are an alpha computation rather than
        /// output-device colour.</param>
        public static IIccTransform? GetDeviceImageTransform(ColorSpaceDetails? colorSpace,
            RenderingIntent imageRenderingIntent, IIccProfile? profile)
        {
            if (colorSpace is null || profile is null)
            {
                return null;
            }

            bool typeEligible = colorSpace.Type is ColorSpace.DeviceGray or ColorSpace.DeviceRGB
                or ColorSpace.DeviceCMYK or ColorSpace.Separation or ColorSpace.DeviceN;

            if (!typeEligible ||
                colorSpace.BaseType is not (ColorSpace.DeviceGray or ColorSpace.DeviceRGB or ColorSpace.DeviceCMYK))
            {
                return null;
            }

            // The device colour space must be expressible in the output intent's colour space: either the
            // component counts match, or it is DeviceGray, which expands neutrally into a 3-/4-component
            // profile exactly as TryMapDeviceToProfileComponents does for a single colour. Other mismatches
            // (e.g. DeviceRGB with a CMYK output intent) keep their built-in conversion.
            bool canManage = profile.NumberOfComponents == colorSpace.BaseNumberOfColorComponents
                || (colorSpace.BaseType == ColorSpace.DeviceGray && profile.NumberOfComponents is 3 or 4);

            if (!canManage)
            {
                return null;
            }

            if (!profile.TryGetTransform(imageRenderingIntent, out var transform))
            {
                return null;
            }

            // canManage above admits exactly one mismatch: a single DeviceGray channel against a 3- or
            // 4-component profile. Hand back a transform that consumes the image's own components so no
            // caller has to notice.
            return transform.NumberOfComponents == colorSpace.BaseNumberOfColorComponents
                ? transform
                : new DeviceGrayExpandingTransform(transform);
        }

        /// <summary>
        /// Presents a 3- or 4-component profile transform as the single-component one a DeviceGray image
        /// needs, expanding each grey neutrally exactly as <see cref="TryMapDeviceToProfileComponents"/> does
        /// for a single colour.
        /// <para>
        /// A one-component source has only 256 possible values, so the packed path converts those 256 once
        /// and then reads the answers off, rather than expanding every pixel into a 3- or 4-times larger
        /// buffer and pushing all of it through the profile. For any image bigger than a thumbnail that is
        /// the difference between 256 conversions and one per pixel.
        /// </para>
        /// </summary>
        private sealed class DeviceGrayExpandingTransform : IIccTransform
        {
            private const int Levels = 256;

            private readonly IIccTransform inner;

            /// <summary>
            /// sRGB for each of the 256 greys, three bytes apiece, built on first use because the scalar
            /// entry point never needs it.
            /// </summary>
            private byte[]? lookup;

            public DeviceGrayExpandingTransform(IIccTransform inner)
            {
                this.inner = inner;
            }

            /// <summary>
            /// One: this only ever stands in for DeviceGray, the sole mismatch
            /// <see cref="GetDeviceImageTransform"/> permits.
            /// </summary>
            public int NumberOfComponents => 1;

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values)
                => TryMapDeviceToProfileComponents(ColorSpace.DeviceGray, values, inner.NumberOfComponents, out var mapped)
                    ? inner.ToRgb(mapped)
                    : inner.ToRgb(values);

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb)
            {
                byte[] table = lookup ??= BuildLookup();

                for (int p = 0; p < src.Length; p++)
                {
                    int entry = src[p] * 3;
                    int i = p * 3;

                    dstRgb[i] = table[entry];
                    dstRgb[i + 1] = table[entry + 1];
                    dstRgb[i + 2] = table[entry + 2];
                }
            }

            /// <summary>
            /// Convert every grey the profile can be asked about, in one call.
            /// </summary>
            private byte[] BuildLookup()
            {
                int components = inner.NumberOfComponents;
                Span<byte> greys = stackalloc byte[Levels * 4]; // 4 is the widest profile this stands in for
                greys = greys.Slice(0, Levels * components);
                greys.Clear();

                if (components == 4)
                {
                    // grey g -> (0, 0, 0, 1 - g)
                    for (int g = 0; g < Levels; g++)
                    {
                        greys[g * 4 + 3] = (byte)(255 - g);
                    }
                }
                else
                {
                    // grey g -> (g, g, g)
                    for (int g = 0; g < Levels; g++)
                    {
                        int i = g * components;

                        for (int c = 0; c < components; c++)
                        {
                            greys[i + c] = (byte)g;
                        }
                    }
                }

                var table = new byte[Levels * 3];
                inner.Transform(greys, table);
                return table;
            }
        }

        /// <summary>
        /// Extract the device colour components from a device colour, or return <see langword="false"/> when
        /// the colour is not a device colour matching <paramref name="colorSpaceType"/>.
        /// </summary>
        private static bool TryGetDeviceComponents(IColor? color, ColorSpace colorSpaceType, out double[] values)
        {
            switch (colorSpaceType)
            {
                case ColorSpace.DeviceGray when color is GrayColor gray:
                    values = [gray.Gray];
                    return true;
                case ColorSpace.DeviceRGB when color is RGBColor rgb:
                    values = [rgb.R, rgb.G, rgb.B];
                    return true;
                case ColorSpace.DeviceCMYK when color is CMYKColor cmyk:
                    values = [cmyk.C, cmyk.M, cmyk.Y, cmyk.K];
                    return true;
                default:
                    values = Array.Empty<double>();
                    return false;
            }
        }

        /// <summary>
        /// Map device colour values onto the output intent profile's colour space. A device space whose
        /// component count already matches the profile passes through unchanged. <see cref="ColorSpace.DeviceGray"/>
        /// is expanded neutrally into a 3- or 4-component profile.
        /// </summary>
        public static bool TryMapDeviceToProfileComponents(ColorSpace deviceType, ReadOnlySpan<double> values, int profileComponents, out ReadOnlySpan<double> mapped)
        {
            if (values.Length == profileComponents)
            {
                mapped = values;
                return true;
            }

            if (deviceType == ColorSpace.DeviceGray && values.Length == 1)
            {
                double grey = values[0];
                mapped = profileComponents switch
                {
                    3 => new double[] { grey, grey, grey },
                    4 => new double[] { 0.0, 0.0, 0.0, 1.0 - grey },
                    _ => []
                };

                return !mapped.IsEmpty;
            }

            mapped = [];
            return false;
        }
    }
}
