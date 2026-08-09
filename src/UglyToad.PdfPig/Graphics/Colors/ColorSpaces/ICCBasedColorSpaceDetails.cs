namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Functions;
    using Icc;

    /// <summary>
    /// The ICCBased color space is one of the CIE-based color spaces supported in PDFs. These color spaces
    /// enable a page description to specify color values in a way that is related to human visual perception.
    /// The goal is for the same color specification to produce consistent results on different output devices,
    /// within the limitations of each device.
    /// <para>
    /// Currently support for this color space is limited in PdfPig. Calculations will only be based on
    /// the color space of <see cref="AlternateColorSpace"/>.
    /// </para>
    /// </summary>
    public sealed class ICCBasedColorSpaceDetails : ColorSpaceDetails
    {
        private readonly bool isLabInput;

        /// <summary>
        /// The number of color components in the color space described by the ICC profile data.
        /// This number shall match the number of components actually in the ICC profile.
        /// Valid values are 1, 3 and 4.
        /// </summary>
        public override int NumberOfColorComponents { get; }

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents { get; }

        /// <summary>
        /// An alternate color space that can be used in case the one specified in the stream data is not
        /// supported. Non-conforming readers may use this color space. The alternate color space may be any
        /// valid color space (except a Pattern color space). If this property isn't explicitly set during
        /// construction, it will assume one of the color spaces, DeviceGray, DeviceRGB or DeviceCMYK depending
        /// on whether the value of <see cref="NumberOfColorComponents"/> is 1, 3 or respectively.
        /// <para>
        /// Conversion of the source color values should not be performed when using the alternate color space.
        /// Color values within the range of the ICCBased color space might not be within the range of the
        /// alternate color space. In this case, the nearest values within the range of the alternate space
        /// must be substituted.
        /// </para>
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
        /// and <see cref="BaseType"/> reports <see cref="ColorSpace.DeviceRGB"/>. Use
        /// <see cref="GetTransformWithFallback(RenderingIntent)"/> to obtain an intent-bound transform.
        /// </summary>
        public IIccProfile? IccProfile { get; }

        /// <summary>
        /// Create a new <see cref="ICCBasedColorSpaceDetails"/>.
        /// </summary>
        internal ICCBasedColorSpaceDetails(int numberOfColorComponents,
            ColorSpaceDetails? alternateColorSpaceDetails,
            IReadOnlyList<double>? range,
            XmpMetadata? metadata,
            ReadOnlyMemory<byte> profileData,
            IIccProfileService? iccService)
            : base(ColorSpace.ICCBased)
        {
            if (numberOfColorComponents != 1 && numberOfColorComponents != 3 && numberOfColorComponents != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfColorComponents), "must be 1, 3 or 4");
            }

            NumberOfColorComponents = numberOfColorComponents;
            AlternateColorSpace = alternateColorSpaceDetails ??
                (NumberOfColorComponents == 1 ? DeviceGrayColorSpaceDetails.Instance :
                NumberOfColorComponents == 3 ? DeviceRgbColorSpaceDetails.Instance : DeviceCmykColorSpaceDetails.Instance);

            Metadata = metadata;

            if (!profileData.IsEmpty && iccService is not null &&
                iccService.TryGetProfile(profileData, out var profile) &&
                profile.NumberOfComponents == NumberOfColorComponents &&
                // We need to make sure the icc profile will at least fall back with RelativeColorimetric to be valid
                profile.TryGetTransform(RenderingIntent.RelativeColorimetric, out _))
            {
                IccProfile = profile;
            }

            Range = range ??
                Enumerable.Range(0, numberOfColorComponents).Select(x => new[] { 0.0, 1.0 }).SelectMany(x => x).ToArray();
            if (Range.Count != 2 * numberOfColorComponents)
            {
                throw new ArgumentOutOfRangeException(nameof(range), range,
                    $"Must consist of exactly {2 * numberOfColorComponents} (2 x NumberOfColorComponents), but was passed {range?.Count ?? 0}");
            }

            if (IccProfile is not null)
            {
                BaseType = ColorSpace.DeviceRGB;
                BaseNumberOfColorComponents = 3;
                isLabInput = IccProfile.IsLabInput;
            }
            else
            {
                BaseType = AlternateColorSpace.BaseType;
                BaseNumberOfColorComponents = NumberOfColorComponents;
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
        /// The ICC.1 encoding range of the L*a*b* data colour space: L* in [0,100], a* and b* in [-128,127].
        /// </summary>
        private static readonly double[] LabRange = [0.0, 100.0, -128.0, 127.0, -128.0, 127.0];

        private void ClipForProfile(ReadOnlySpan<double> values, Span<double> destination)
        {
            IReadOnlyList<double> bounds = isLabInput && destination.Length <= 3 ? LabRange : Range;

            for (int c = 0; c < destination.Length; c++)
            {
                int i = 2 * c;
                destination[c] = PdfFunction.ClipToRange(values[c], bounds[i], bounds[i + 1]);
            }
        }

        /// <inheritdoc/>
        internal override double[] Process(double[] values, RenderingIntent intent)
        {
            Span<double> operands = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4
            Normalise(values, operands);

            if (IccProfile is not null)
            {
                IIccTransform? t = GetTransformWithFallback(intent);
                if (t is not null)
                {
                    Span<double> forProfile = stackalloc double[NumberOfColorComponents];
                    ClipForProfile(operands, forProfile);

                    var (r, g, b) = t.ToRgb(forProfile);
                    return [r, g, b];
                }
            }

            double[] clipped = new double[NumberOfColorComponents];
            ClipToRange(operands, clipped);

            return AlternateColorSpace.Process(clipped, intent);
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
        {
            if (values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            Span<double> buffer = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4

            if (IccProfile is not null)
            {
                var t = GetTransformWithFallback(intent);
                if (t is not null)
                {
                    ClipForProfile(values, buffer);
                    var (r, g, b) = t.ToRgb(buffer);
                    return new RGBColor(r, g, b);
                }
            }

            ClipToRange(values, buffer);
            return AlternateColorSpace.GetColor(buffer, intent);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to any CIE-based colour space shall
            // initialize all components of the corresponding current colour to 0.0 (unless the range of valid
            // values for a given component does not include 0.0, in which case the nearest valid value shall
            // be substituted.)
            double v = PdfFunction.ClipToRange(0.0, Range[0], Range[1]);
            Span<double> buffer = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4
            buffer.Fill(v);
            return GetColor(buffer);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            Span<double> clipped = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4

            if (IccProfile is not null)
            {
                var t = GetTransformWithFallback(intent);
                if (t is not null)
                {
                    ClipForProfile(values, clipped);
                    (r, g, b) = t.ToRgb(clipped);
                    return;
                }
            }

            ClipToRange(values, clipped);
            AlternateColorSpace.GetRgb(clipped, intent, out r, out g, out b);
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
        {
            if (IccProfile is not null)
            {
                var t = GetTransformWithFallback(intent);
                if (t is not null)
                {
                    int pixelCount = decoded.Length / NumberOfColorComponents;
                    byte[] dst = new byte[pixelCount * 3];
                    t.Transform(decoded, dst);
                    return dst;
                }
            }

            return AlternateColorSpace.Transform(decoded, intent);
        }
    }
}
