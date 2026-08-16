namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System;
    using System.Collections.Generic;
    using Core;

    /// <summary>
    /// Colour-manages device colours through a document's output intent (14.11.5, "Output intents", and
    /// 8.6.5.7, "Implicit conversion of CIE-based colour spaces").
    /// <para>
    /// This is opt-in, and <see cref="IIccProfileService.UseOutputIntent"/> is what opts in:
    /// the intent's profile cannot be parsed without a service, so the capability and the decision are the
    /// same thing. When it is off, device colours keep their built-in conversion.
    /// </para>
    /// </summary>
    public static class OutputIntentColorManagement
    {
        /// <summary>
        /// The embedded profile that characterises the output device for the content being processed, or
        /// <see langword="null"/> when nothing does: when <paramref name="service"/> has not
        /// opted in, when the graphics state carries no output intent (an empty list, or
        /// <see langword="null"/> where a consumer has suppressed it, see
        /// <see cref="CurrentGraphicsState.OutputIntents"/>), or when no declared intent carries a
        /// <c>/DestOutputProfile</c>.
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
        /// Convert a device colour through an output intent profile to sRGB, returning <see langword="true"/>
        /// and the managed <see cref="RGBColor"/> when the colour is a device colour (DeviceGray / DeviceRGB
        /// / DeviceCMYK) expressible in the profile's colour space. Returns <see langword="false"/> - so the
        /// caller keeps the built-in conversion - for non-device colours, for profile/device mismatches with
        /// no neutral mapping (e.g. DeviceRGB with a CMYK output intent), and when the transform cannot be
        /// built.
        /// </summary>
        public static bool TryConvert(IColor? color, ColorSpace colorSpaceType, IIccProfile profile,
            RenderingIntent intent, out IColor? managed)
        {
            managed = null;

            if (!TryGetDeviceComponents(color, colorSpaceType, out double[] values) ||
                !TryMapDeviceToProfileComponents(colorSpaceType, values, profile.NumberOfComponents, out var deviceValues) ||
                !profile.TryGetTransform(intent, out var transform) ||
                transform is null)
            {
                return false;
            }

            var (r, g, b) = transform.ToRgb(deviceValues);
            managed = new RGBColor(r, g, b);
            return true;
        }

        /// <summary>
        /// The device colour space a colour finally resolves to, which is what an output intent characterises.
        /// A Separation or DeviceN whose colorant is not on the device paints in its alternate device space -
        /// its current colour is already that alternate's <see cref="GrayColor"/> or <see cref="CMYKColor"/> -
        /// so those key on the alternate. Otherwise the space speaks for itself.
        /// </summary>
        public static ColorSpace GetEffectiveDeviceType(ColorSpaceDetails colorSpace)
            => colorSpace.Type is ColorSpace.Separation or ColorSpace.DeviceN
                ? colorSpace.BaseType
                : colorSpace.Type;

        /// <summary>
        /// The transform that colour-manages a raster image's samples through the output intent, or
        /// <see langword="null"/> when it should keep its built-in conversion. The counterpart of
        /// <see cref="TryConvert"/> for the image path, so that the two cannot disagree about which images
        /// and colour spaces are eligible; applying it to the samples is the consumer's job, since that is
        /// rasterisation.
        /// <para>
        /// Eligible are a direct DeviceGray/DeviceRGB/DeviceCMYK image, and a Separation or DeviceN image
        /// whose alternate is one of those - its samples have already been converted into that alternate
        /// device space before a consumer sees them. ICCBased, Lab, CalRGB/CalGray, Indexed, and
        /// Separation/DeviceN over an ICC alternate all carry their own colour management, so their
        /// <see cref="ColorSpaceDetails.BaseType"/> is not a plain device space and they are skipped.
        /// </para>
        /// </summary>
        /// <param name="colorSpace">The image's colour space, or <see langword="null"/> if it has none.</param>
        /// <param name="imageRenderingIntent">The image's rendering intent.</param>
        /// <param name="outputIntents">The output intents in effect, in array order. Page-scoped, and
        /// <see langword="null"/> inside a soft-mask group, where device values are an alpha computation
        /// rather than output-device colour.</param>
        /// <param name="service">The configured service - see <see cref="GetDeviceProfile"/>.</param>
        public static IIccTransform? GetDeviceImageTransform(ColorSpaceDetails? colorSpace,
            RenderingIntent imageRenderingIntent, IReadOnlyList<OutputIntent>? outputIntents,
            IIccProfileService? service)
        {
            if (service is null || colorSpace is null)
            {
                return null;
            }
            
            var profile = GetDeviceProfile(outputIntents, service);
            if (profile is null)
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

            return profile.TryGetTransform(imageRenderingIntent, out var transform) ? transform : null;
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
        /// is expanded neutrally into a 3- or 4-component profile: grey <c>g</c> becomes <c>(g, g, g)</c> for an
        /// RGB profile, or <c>(0, 0, 0, 1 - g)</c> — the black channel — for a CMYK profile, so grey content
        /// shares the managed space. Other mismatches (notably DeviceRGB with a CMYK output intent, or
        /// DeviceCMYK with an RGB output intent) have no well-defined neutral mapping and return <c>false</c>.
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
