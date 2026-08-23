namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Logging;

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
        /// <summary>
        /// The number of color components in the color space described by the ICC profile data.
        /// This numbers shall match the number of components actually in the ICC profile.
        /// Valid values are 1, 3 and 4.
        /// </summary>
        public override int NumberOfColorComponents { get; }

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => AlternateColorSpace.BaseNumberOfColorComponents;

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
        /// Create a new <see cref="ICCBasedColorSpaceDetails"/>.
        /// </summary>
        internal ICCBasedColorSpaceDetails(int numberOfColorComponents,
            ColorSpaceDetails? alternateColorSpaceDetails,
            IReadOnlyList<double>? range,
            XmpMetadata? metadata,
            ILog? log = null)
            : base(ColorSpace.ICCBased)
        {
            if (!IsValidComponentCount(numberOfColorComponents))
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfColorComponents), "Must be 1, 3 or 4.");
            }

            Metadata = metadata;
            NumberOfColorComponents = numberOfColorComponents;

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

            BaseType = AlternateColorSpace.BaseType;
        }

        /// <summary>
        /// The component counts an ICCBased colour space may have (8.6.5.5).
        /// </summary>
        public static bool IsValidComponentCount(int components) => components is 1 or 3 or 4;

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            // TODO - use ICC profile

            return AlternateColorSpace.Process(values);
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values)
        {
            if (values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            // TODO - use ICC profile

            Span<double> buffer = stackalloc double[values.Length]; // 1, 3 or 4
            for (int c = 0; c < values.Length; c++)
            {
                int i = 2 * c;
                buffer[c] = PdfFunction.ClipToRange(values[c], Range[i], Range[i + 1]);
            }

            return AlternateColorSpace.GetColor(buffer);
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
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            // TODO - use ICC profile

            Span<double> clipped = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4
            for (int c = 0; c < NumberOfColorComponents; c++)
            {
                int i = 2 * c;
                clipped[c] = PdfFunction.ClipToRange(values[c], Range[i], Range[i + 1]);
            }
            AlternateColorSpace.GetRgb(clipped, out r, out g, out b);
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// The alternate colour space owns the conversion, so it owns the range the samples decode into
        /// on the way there.
        /// </para>
        /// </summary>
        public override void GetDefaultDecode(int bitsPerComponent, Span<double> destination)
        {
            if (AlternateColorSpace.NumberOfColorComponents != NumberOfColorComponents)
            {
                base.GetDefaultDecode(bitsPerComponent, destination);
                return;
            }

            AlternateColorSpace.GetDefaultDecode(bitsPerComponent, destination);
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// As with <see cref="GetDefaultDecode"/>, the alternate colour space owns the mapping because it
        /// owns the conversion that follows it.
        /// </para>
        /// </summary>
        internal override void DecodeRawComponents(ReadOnlySpan<byte> raw, Span<double> destination)
            => AlternateColorSpace.DecodeRawComponents(raw, destination);

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            // TODO - use ICC profile

            return AlternateColorSpace.Transform(decoded);
        }
    }
}
