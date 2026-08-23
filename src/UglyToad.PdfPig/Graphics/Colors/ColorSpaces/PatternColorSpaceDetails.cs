namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// Pattern color space.
    /// </summary>
    public sealed class PatternColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// The pattern dictionary.
        /// </summary>
        public IReadOnlyDictionary<NameToken, PatternColor> Patterns { get; }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="PatternColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        public override int NumberOfColorComponents
            => throw new InvalidOperationException("Cannot be called for PatternColorSpaceDetails.");

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Valid for Uncoloured Tiling Patterns. Will throw a <see cref="InvalidOperationException"/> otherwise.
        /// </para>
        /// </summary>
        public override int BaseNumberOfColorComponents => UnderlyingColourSpace!.NumberOfColorComponents;

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// A pattern is selected by name and converts nothing itself. Only the operands an uncoloured tiling
        /// pattern carries alongside the name are converted, through <see cref="UnderlyingColourSpace"/>;
        /// a coloured tiling pattern or a shading pattern declares none.
        /// </para>
        /// </summary>
        public override bool RenderingIntentAffectsOutput
            => UnderlyingColourSpace?.RenderingIntentAffectsOutput ?? false;

        /// <summary>
        /// The underlying color space for Uncoloured Tiling Patterns.
        /// </summary>
        public ColorSpaceDetails? UnderlyingColourSpace { get; }

        /// <summary>
        /// Create a new <see cref="PatternColorSpaceDetails"/>.
        /// </summary>
        /// <param name="patterns">The patterns.</param>
        /// <param name="underlyingColourSpace">The underlying colour space for Uncoloured Tiling Patterns.</param>
        public PatternColorSpaceDetails(IReadOnlyDictionary<NameToken, PatternColor> patterns, ColorSpaceDetails underlyingColourSpace)
            : base(ColorSpace.Pattern)
        {
            Patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
            UnderlyingColourSpace = underlyingColourSpace;
        }

        /// <summary>
        /// Get the corresponding <see cref="PatternColor"/>.
        /// </summary>
        /// <param name="name"></param>
        public PatternColor GetColor(NameToken name)
        {
            return Patterns[name];
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="PatternColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        internal override double[] Process(double[] values, RenderingIntent intent)
            => throw new InvalidOperationException("Cannot be called for PatternColorSpaceDetails.");

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="PatternColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// Use <see cref="GetColor(NameToken)"/> instead.
        /// </para>
        /// </summary>
        public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
            => throw new InvalidOperationException("Cannot be called for PatternColorSpaceDetails.");

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent, out double r, out double g, out double b)
            => throw new InvalidOperationException("Cannot be called for PatternColorSpaceDetails.");

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>Always returns <c>null</c>.</returns>
        public override IColor? GetInitializeColor(RenderingIntent intent)
        {
            return null;
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="PatternColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
            => throw new InvalidOperationException("Cannot be called for PatternColorSpaceDetails.");
    }
}
