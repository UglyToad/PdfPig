namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Functions;
    using Tokens;

    /// <summary>
    /// A Separation color space provides a means for specifying the use of additional colorants or
    /// for isolating the control of individual color components of a device color space for a subtractive device.
    /// When such a space is the current color space, the current color is a single-component value, called a tint,
    /// that controls the application of the given colorant or color components only.
    /// </summary>
    public sealed class SeparationColorSpaceDetails : ColorSpaceDetails
    {
        private readonly ConcurrentDictionary<(double Tint, RenderingIntent Intent), IColor> cache = new();

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 1;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => AlternateColorSpace.BaseNumberOfColorComponents;

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// The tint transform is a function of the tint alone; the intent only reaches
        /// <see cref="AlternateColorSpace"/>, which the result is converted through.
        /// </para>
        /// </summary>
        public override bool RenderingIntentAffectsOutput => AlternateColorSpace.RenderingIntentAffectsOutput;

        /// <summary>
        /// Specifies the name of the colorant that this Separation color space is intended to represent.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The special colorant name All refers collectively to all colorants available on an output device,
        /// including those for the standard process colorants.
        /// </para>
        /// <para>
        /// The special colorant name None never produces any visible output.
        /// Painting operations in a Separation space with this colorant name have no effect on the current page.
        /// </para>
        /// </remarks>
        public NameToken Name { get; }

        /// <summary>
        /// If the colorant name associated with a Separation color space does not correspond to a colorant available on the device,
        /// the application arranges for subsequent painting operations to be performed in an alternate color space.
        /// The intended colors can be approximated by colors in a device or CIE-based color space
        /// which are then rendered with the usual primary or process colorants.
        /// </summary>
        public ColorSpaceDetails AlternateColorSpace { get; }

        /// <summary>
        /// During subsequent painting operations, an application calls this function to transform a tint value into
        /// color component values in the alternate color space.
        /// The function is called with the tint value and must return the corresponding color component values.
        /// That is, the number of components and the interpretation of their values depend on the <see cref="AlternateColorSpace"/>.
        /// </summary>
        public PdfFunction TintFunction { get; }

        /// <summary>
        /// Create a new <see cref="SeparationColorSpaceDetails"/>.
        /// </summary>
        public SeparationColorSpaceDetails(NameToken name,
            ColorSpaceDetails alternateColorSpaceDetails,
            PdfFunction tintFunction)
            : base(ColorSpace.Separation)
        {
            Name = name;
            AlternateColorSpace = alternateColorSpaceDetails;
            TintFunction = tintFunction;
            BaseType = AlternateColorSpace.BaseType;
        }

        /// <inheritdoc/>
        internal override double[] Process(double[] values, RenderingIntent intent)
        {
            var evaled = TintFunction.Eval(values[0]);
            return AlternateColorSpace.Process(evaled, intent);
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
        {
            if (values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            // TODO - we ignore the name for now

            var key = (values[0], intent);
            if (cache.TryGetValue(key, out var color))
            {
                return color;
            }

            color = TintColorSpaceDetailsHelper.GetColorViaTint(TintFunction, AlternateColorSpace, values, intent);

            cache.TryAdd(key, color);
            return color;
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> values, RenderingIntent intent)
        {
            var colorCache = new Dictionary<byte, double[]>(values.Length);
            var transformed = new byte[values.Length * BaseNumberOfColorComponents];
            int k = 0;

            for (var i = 0; i < values.Length; ++i)
            {
                byte b = values[i];
                if (!colorCache.TryGetValue(b, out double[]? colors))
                {
                    colors = Process([b / 255.0], intent);
                    colorCache[b] = colors;
                }

                for (int c = 0; c < colors.Length; ++c)
                {
                    transformed[k++] = ConvertToByte(colors[c]);
                }
            }

            return transformed;
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor(RenderingIntent intent)
        {
            // The initial value for both the stroking and nonstroking colour in the graphics state shall be 1.0.
            return GetColor([1.0], intent);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            TintColorSpaceDetailsHelper.GetRgbViaTint(TintFunction, AlternateColorSpace, values.Slice(0, 1), intent,
                out r, out g, out b);
        }
    }
}
