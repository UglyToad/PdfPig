namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Functions;
    using Icc;
    using Logging;

    /// <summary>
    /// The ICCBased color space is one of the CIE-based color spaces supported in PDFs. These color spaces
    /// enable a page description to specify color values in a way that is related to human visual perception.
    /// The goal is for the same color specification to produce consistent results on different output devices,
    /// within the limitations of each device.
    /// </summary>
    public sealed class ICCBasedColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// See <see cref="GetProfileRanges"/>. Null whenever <see cref="Range"/> is the authority.
        /// </summary>
        private readonly IReadOnlyList<double>? profileRanges;

        /// <summary>
        /// The number of color components in the color space described by the ICC profile data.
        /// This number shall match the number of components actually in the ICC profile.
        /// Valid values are 1, 3 and 4.
        /// </summary>
        public override int NumberOfColorComponents { get; }

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents { get; }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// <c>true</c> whenever a profile is in use: <see cref="GetTransformWithFallback"/> resolves a different
        /// <see cref="IIccTransform"/> per intent. Without one it returns null and every conversion falls
        /// through to <see cref="AlternateColorSpace"/>, whose own answer then applies.
        /// </para>
        /// </summary>
        public override bool RenderingIntentAffectsOutput
            => IccProfile is not null || AlternateColorSpace.RenderingIntentAffectsOutput;

        /// <summary>
        /// An alternate color space that can be used in case the one specified in the stream data is not
        /// supported. Non-conforming readers may use this color space. The alternate color space may be any
        /// valid color space (except a Pattern color space).
        /// </summary>
        public ColorSpaceDetails AlternateColorSpace { get; }

        /// <summary>
        /// A list of 2 x <see cref="NumberOfColorComponents"/> numbers [min0 max0  min1 max1  ...] that
        /// specifies the minimum and maximum valid values of the corresponding color components. These
        /// values must match the information in the ICC profile. Default value: [0.0 1.0  0.0 1.0  ...].
        /// </summary>
        public IReadOnlyList<double> Range { get; }

        /// <summary>
        /// An optional metadata stream that contains metadata for the color space.
        /// </summary>
        public XmpMetadata? Metadata { get; }

        /// <summary>
        /// The resolved ICC profile, or <c>null</c> when no <see cref="IIccProfileService"/> was configured,
        /// or the service failed to parse the profile. When non-null, color conversions produce sRGB output
        /// and <see cref="BaseType"/> reports <see cref="ColorSpace.DeviceRGB"/>.
        /// </summary>
        public IIccProfile? IccProfile { get; }

        /// <summary>
        /// Create a new <see cref="ICCBasedColorSpaceDetails"/>.
        /// </summary>
        internal ICCBasedColorSpaceDetails(int numberOfColorComponents,
            ColorSpaceDetails? alternateColorSpaceDetails,
            IReadOnlyList<double>? range,
            XmpMetadata? metadata,
            IIccProfile? profile,
            ILog? log = null)
            : base(ColorSpace.ICCBased)
        {
            Metadata = metadata;

            // 8.6.5.5 requires /N to match the profile, and when they disagree the profile is the one that
            // cannot be wrong about itself: a transform reads its own number of components no matter what
            // the dictionary claims.
            if (profile is not null && profile.NumberOfComponents != numberOfColorComponents)
            {
                if (IsValidComponentCount(profile.NumberOfComponents))
                {
                    log?.Warn($"Using {profile.NumberOfComponents} components from the ICC profile instead of the " +
                              $"{numberOfColorComponents} declared by the /N entry of the ICCBased colour space.");
                    numberOfColorComponents = profile.NumberOfComponents;
                }
                else
                {
                    log?.Warn($"The ICC profile declares {profile.NumberOfComponents} components, which no ICCBased " +
                              "colour space may have; ignoring the profile and using the alternate colour space.");
                    profile = null;
                }
            }

            if (!IsValidComponentCount(numberOfColorComponents))
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfColorComponents), "must be 1, 3 or 4");
            }

            NumberOfColorComponents = numberOfColorComponents;

            // NumberOfColorComponents needs to be set before using IsUsable(...).
            // We need to make sure the icc profile will at least fall back with RelativeColorimetric to be valid
            if (profile is not null && !IsUsable(profile, NumberOfColorComponents, log))
            {
                log?.Warn("The ICC profile resolved but could not convert a colour; using the alternate colour space.");
                profile = null;
            }

            IccProfile = profile;

            // The alternate stands in for the profile and is handed the very same operands, so one of the
            // wrong width cannot be evaluated at all. This is also where a /N corrected above lands: an
            // alternate chosen against the declared /N may no longer fit.
            if (alternateColorSpaceDetails is not null &&
                alternateColorSpaceDetails.NumberOfColorComponents != NumberOfColorComponents)
            {
                log?.Warn($"The /Alternate colour space of an ICCBased colour space takes " +
                          $"{alternateColorSpaceDetails.NumberOfColorComponents} components where the colour space has " +
                          $"{NumberOfColorComponents}; ignoring it and using the implied device colour space.");

                alternateColorSpaceDetails = null;
            }

            AlternateColorSpace = alternateColorSpaceDetails ??
                (NumberOfColorComponents == 1 ? DeviceGrayColorSpaceDetails.Instance :
                NumberOfColorComponents == 3 ? DeviceRgbColorSpaceDetails.Instance :
                DeviceCmykColorSpaceDetails.Instance);

            if (range is not null && range.Count != 2 * NumberOfColorComponents)
            {
                log?.Warn($"The /Range of an ICCBased colour space has {range.Count} entries where " +
                          $"{2 * NumberOfColorComponents} (2 x {NumberOfColorComponents}) are required; using the default.");

                range = null;
            }

            Range = range ?? Enumerable.Range(0, NumberOfColorComponents)
                .Select(x => new[] { 0.0, 1.0 })
                .SelectMany(x => x)
                .ToArray();

            if (IccProfile is not null)
            {
                BaseNumberOfColorComponents = 3;
                BaseType = ColorSpace.DeviceRGB;
                profileRanges = GetProfileRanges(IccProfile, NumberOfColorComponents);
            }
            else
            {
                // Not NumberOfColorComponents: the alternate is what Transform delegates to, so it is the
                // alternate's own base width that says how many components come back.
                BaseNumberOfColorComponents = AlternateColorSpace.BaseNumberOfColorComponents;
                BaseType = AlternateColorSpace.BaseType;
            }
        }

        /// <summary>
        /// The component counts an ICCBased colour space may have (8.6.5.5).
        /// </summary>
        private static bool IsValidComponentCount(int components) => components is 1 or 3 or 4;

        /// <summary>
        /// The profile's own component ranges, or <c>null</c> when it encodes everything in <c>[0, 1]</c>
        /// and the colour space's <c>/Range</c> entry should therefore be left in charge.
        /// </summary>
        private static IReadOnlyList<double>? GetProfileRanges(IIccProfile profile, int numberOfColorComponents)
        {
            var ranges = profile.ComponentRanges;

            if (ranges is null || ranges.Count != 2 * numberOfColorComponents)
            {
                return null;
            }

            for (int i = 0; i < ranges.Count; i += 2)
            {
                if (ranges[i] != 0.0 || ranges[i + 1] != 1.0)
                {
                    return ranges;
                }
            }

            return null;
        }

        /// <summary>
        /// Whether the profile can actually convert, not merely hand out a transform.
        /// </summary>
        private static bool IsUsable(IIccProfile profile, int numberOfColorComponents, ILog? log)
        {
            // Obtaining an IIccTransform proves nothing: the interface lets a service build its transforms lazily,
            // so the work that a malformed profile fails at may not have happened yet. Both conversion entry points
            // are therefore exercised here, inside the construction that can still choose AlternateColorSpace instead.
            //
            // PDFBox does exactly this for the same reason, see PDICCBased.loadICCProfile, which calls both 'toRGB'
            // and 'new ComponentColorModel' inside the try that falls back, each citing a different profile that parsed
            // cleanly and then threw on use (PDFBOX-1295, 1740, 3610, 4015, 5563).

            if (!profile.TryGetTransform(RenderingIntent.RelativeColorimetric, out var transform))
            {
                return false;
            }

            try
            {
                // Zero is in range for every data colour space a profile may declare, L*a*b* included.
                Span<double> components = stackalloc double[numberOfColorComponents]; // 1, 3 or 4
                transform.ToRgb(components);

                // The packed-byte path is a separate implementation and fails separately.
                Span<byte> onePixel = stackalloc byte[numberOfColorComponents];
                Span<byte> rgb = stackalloc byte[3];
                transform.Transform(onePixel, rgb);

                return true;
            }
            catch (Exception ex)
            {
                log?.Error("ICC profile is malformed and is not usable. Falling back to alternate color space.", ex);
                return false;
            }
        }

        internal IIccTransform? GetTransformWithFallback(RenderingIntent intent)
        {
            if (IccProfile is null)
            {
                return null;
            }

            if (intent != RenderingIntent.RelativeColorimetric &&
                IccProfile.TryGetTransform(intent, out var t))
            {
                return t;
            }

            return IccProfile.TryGetTransform(RenderingIntent.RelativeColorimetric, out var rct) ? rct : null;
        }

        /// <summary>
        /// Convert through <paramref name="transform"/>, reporting <see langword="false"/> rather than
        /// throwing so the caller can fall back to <see cref="AlternateColorSpace"/> for this colour.
        /// A failure is not held against the profile: the next colour tries it again.
        /// </summary>
        private static bool TryToRgb(IIccTransform transform, ReadOnlySpan<double> components,
            out double r, out double g, out double b)
        {
            try
            {
                (r, g, b) = transform.ToRgb(components);
                return true;
            }
            catch
            {
                r = g = b = 0.0;
                return false;
            }
        }

        /// <summary>
        /// Clip the components to <see cref="Range"/>.
        /// </summary>
        private void ClipToRange(ReadOnlySpan<double> values, Span<double> destination)
        {
            for (int c = 0; c < destination.Length; c++)
            {
                int i = 2 * c;
                destination[c] = PdfFunction.ClipToRange(values[c], Range[i], Range[i + 1]);
            }
        }

        /// <summary>
        /// The bounds the profile path clips against, and the range a sample byte is understood to span:
        /// the profile's own encoding when it declares one, otherwise the colour space's <c>/Range</c>.
        /// </summary>
        private IReadOnlyList<double> EffectiveRanges => profileRanges ?? Range;

        private void ClipForProfile(ReadOnlySpan<double> values, Span<double> destination)
        {
            IReadOnlyList<double> bounds = EffectiveRanges;

            for (int c = 0; c < destination.Length; c++)
            {
                int i = 2 * c;
                destination[c] = PdfFunction.ClipToRange(values[c], bounds[i], bounds[i + 1]);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Taken from the resolved profile's own encoding, which is the only thing that knows an L*a*b*
        /// profile's L* runs to 100. With no profile the alternate colour space decides.
        /// </para>
        /// </summary>
        public override void GetDefaultDecode(int bitsPerComponent, Span<double> destination)
        {
            if (profileRanges is null)
            {
                if (IccProfile is null)
                {
                    AlternateColorSpace.GetDefaultDecode(bitsPerComponent, destination);
                    return;
                }

                base.GetDefaultDecode(bitsPerComponent, destination);
                return;
            }

            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = profileRanges[i];
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Colour-table bytes reach this colour space's components through its own ranges, so an Indexed
        /// space over an L*a*b* profile decodes L* into [0, 100] rather than [0, 1]. Without a profile the
        /// alternate colour space owns the mapping, as it owns the conversion that follows it.
        /// </para>
        /// </summary>
        internal override void DecodeRawComponents(ReadOnlySpan<byte> raw, Span<double> destination)
        {
            if (profileRanges is null)
            {
                if (IccProfile is null)
                {
                    AlternateColorSpace.DecodeRawComponents(raw, destination);
                    return;
                }

                base.DecodeRawComponents(raw, destination);
                return;
            }

            for (int i = 0; i < raw.Length; i++)
            {
                int c = 2 * (i % NumberOfColorComponents);
                double min = profileRanges[c];
                destination[i] = min + (raw[i] / 255.0) * (profileRanges[c + 1] - min);
            }
        }

        /// <inheritdoc/>
        internal override double[] Process(double[] values, RenderingIntent intent)
        {
            Span<double> operands = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4
            Normalise(values, operands);

            IIccTransform? transform = GetTransformWithFallback(intent);
            if (transform is not null)
            {
                Span<double> forProfile = stackalloc double[NumberOfColorComponents];
                ClipForProfile(operands, forProfile);

                if (TryToRgb(transform, forProfile, out double r, out double g, out double b))
                {
                    return [r, g, b];
                }
            }

            double[] clipped = new double[NumberOfColorComponents];
            ClipToRange(operands, clipped);

            if (IccProfile is null)
            {
                // BaseType and BaseNumberOfColorComponents are the alternate's own, so its components are
                // already the ones the caller is expecting.
                return AlternateColorSpace.Process(clipped, intent);
            }

            // A profile is in use, so BaseType is DeviceRGB and three components are what a caller sizes its
            // buffers from, however this particular colour ended up being produced. Reaching the alternate
            // here says the profile could not convert this colour, not that the colour space has changed
            // shape: a CMYK alternate's four components would overrun the caller by one per sample.
            AlternateColorSpace.GetRgb(clipped, intent, out double red, out double green, out double blue);
            return [red, green, blue];
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
        {
            if (values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            Span<double> buffer = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4

            var transform = GetTransformWithFallback(intent);
            if (transform is not null)
            {
                ClipForProfile(values, buffer);
                if (TryToRgb(transform, buffer, out double r, out double g, out double b))
                {
                    return new RGBColor(r, g, b);
                }
            }

            ClipToRange(values, buffer);
            return AlternateColorSpace.GetColor(buffer, intent);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor(RenderingIntent intent)
        {
            // Setting the current stroking or nonstroking colour space to any CIE-based colour space shall
            // initialize all components of the corresponding current colour to 0.0 (unless the range of valid
            // values for a given component does not include 0.0, in which case the nearest valid value shall
            // be substituted.)
            double v = PdfFunction.ClipToRange(0.0, Range[0], Range[1]);
            Span<double> buffer = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4
            buffer.Fill(v);
            return GetColor(buffer, intent);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            Span<double> clipped = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4

            var transform = GetTransformWithFallback(intent);
            if (transform is not null)
            {
                ClipForProfile(values, clipped);
                if (TryToRgb(transform, clipped, out r, out g, out b))
                {
                    return;
                }
            }

            ClipToRange(values, clipped);
            AlternateColorSpace.GetRgb(clipped, intent, out r, out g, out b);
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
        {
            var transform = GetTransformWithFallback(intent);
            if (transform is not null)
            {
                int pixelCount = decoded.Length / NumberOfColorComponents;
                byte[] dst = new byte[pixelCount * 3];

                try
                {
                    // pixelCount truncates, so the samples are sliced to the whole pixels dst has room for:
                    // IIccTransform.Transform is contracted for src.Length == pixelCount x NumberOfComponents,
                    // and an implementation holding to that either throws on a trailing partial pixel or runs
                    // off the end of dst.
                    transform.Transform(decoded.Slice(0, pixelCount * NumberOfColorComponents), dst);
                    return dst;
                }
                catch
                {
                    // The source is read-only by contract, so it is still intact for the alternate.
                }
            }

            if (IccProfile is null)
            {
                // As in Process: with no profile the alternate's own base is this colour space's base, so
                // its output is already the right width.
                return AlternateColorSpace.Transform(decoded, intent);
            }

            // And as in Process, a profile in use pins the base to DeviceRGB. The alternate's own base may
            // be any width - a CMYK alternate hands back four bytes a pixel where PngFromPdfImageFactory
            // reads three - so the samples go through a colour at a time instead, the way Lab does.
            return TransformThroughAlternate(decoded, intent);
        }

        /// <summary>
        /// Convert image samples to RGB one colour at a time, for the profile-in-use case where
        /// <see cref="AlternateColorSpace"/>'s packed output would be the wrong width.
        /// <para>
        /// Each colour goes back through <see cref="GetRgb"/> rather than straight to the alternate, so a
        /// profile whose packed entry point failed is still asked for the colours its scalar one can convert
        /// - the same "a failure is not held against the profile" rule the other entry points follow.
        /// </para>
        /// </summary>
        private byte[] TransformThroughAlternate(Span<byte> decoded, RenderingIntent intent)
        {
            int pixelCount = decoded.Length / NumberOfColorComponents;
            var transformed = new byte[pixelCount * 3];

            Span<double> components = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4
            int index = 0;

            for (int i = 0; i < pixelCount; i++)
            {
                DecodeRawComponents(decoded.Slice(i * NumberOfColorComponents, NumberOfColorComponents), components);
                GetRgb(components, intent, out double r, out double g, out double b);

                transformed[index++] = ConvertToByte(r);
                transformed[index++] = ConvertToByte(g);
                transformed[index++] = ConvertToByte(b);
            }

            return transformed;
        }
    }
}
