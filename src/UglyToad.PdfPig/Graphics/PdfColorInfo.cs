namespace UglyToad.PdfPig.Graphics
{
    using Colors;
    using Tokens;

    /// <summary>
    /// A current colour, together with the Pattern it was selected through when it was selected that way.
    /// <para>
    /// A Pattern colour is two things at once: the pattern itself, which is what the colour <i>is</i>, and
    /// (for an uncoloured tiling pattern) the colour its cell is painted in, which is selected by the same
    /// operator from a different colour space.
    /// </para>
    /// </summary>
    internal readonly struct PdfColorInfo
    {
        /// <summary>
        /// What the operands convert to. For a Pattern colour this is the <i>underlying</i> colour an
        /// uncoloured tiling pattern paints with, and <see cref="patternColor"/> is the colour itself.
        /// </summary>
        private readonly IColor? color;

        /// <summary>
        /// The Pattern colour, when one was selected; <see langword="null"/> otherwise. A pattern is chosen
        /// by name rather than by component values, so it never converts.
        /// </summary>
        private readonly IColor? patternColor;

        private PdfColorInfo(IColor? color, IColor? patternColor)
        {
            this.color = color;
            this.patternColor = patternColor;
        }

        /// <summary>
        /// A colour with nothing behind it, which is what a consumer handing over an already-converted
        /// colour stores.
        /// </summary>
        public static PdfColorInfo Fixed(IColor? color) => new(color, null);

        /// <summary>
        /// A colour selected in <paramref name="colorSpace"/>.
        /// </summary>
        /// <param name="colorSpace">The colour space the operands belong to.</param>
        /// <param name="operands">The selected component values, or <see langword="null"/> for the colour
        /// space's initial colour.</param>
        public static PdfColorInfo FromOperands(ColorSpaceDetails colorSpace, double[]? operands)
            => new(Convert(colorSpace, operands), null);

        /// <summary>
        /// A Pattern colour selected by <c>SCN</c>/<c>scn</c>. The pattern itself comes from a name, but an
        /// <b>uncoloured</b> tiling pattern (<c>/PaintType 2</c>) also carries the colour its content is
        /// painted in, as operands in the <i>underlying</i> colour space that the Pattern space was declared
        /// with (8.7.3.3). Those operands are converted here and read back through
        /// <see cref="UnderlyingColor"/>.
        /// </summary>
        /// <param name="patternColorSpace">The Pattern colour space the name is resolved against.</param>
        /// <param name="patternName">The name of an entry in the <c>/Pattern</c> subdictionary of the current resource dictionary.</param>
        /// <param name="operands">The component values accompanying the name, in the underlying colour space.
        /// Empty or <see langword="null"/> is the normal case for a coloured tiling pattern and for a shading
        /// pattern, neither of which has an underlying colour at all. For an uncoloured tiling pattern it is
        /// malformed, the operator owes a colour and supplied none, and the underlying space's own initial
        /// colour stands in for it.</param>
        public static PdfColorInfo ForPattern(PatternColorSpaceDetails patternColorSpace, NameToken patternName,
            double[]? operands)
        {
            // Normalise "no operands" to null so that the underlying space derives its own initial colour
            // rather than being asked to convert an empty component list.
            if (operands is { Length: 0 })
            {
                operands = null;
            }

            var patternColor = patternColorSpace.GetColor(patternName);
            var underlyingColorSpace = GetUnderlyingColorSpace(patternColorSpace, patternColor, operands);

            // No underlying colour at all: a coloured tiling pattern or a shading pattern.
            return underlyingColorSpace is null
                ? new PdfColorInfo(null, patternColor)
                : new PdfColorInfo(TryConvert(underlyingColorSpace, operands), patternColor);
        }

        /// <summary>
        /// The underlying colour space to convert <paramref name="operands"/> through, or
        /// <see langword="null"/> when there is no usable one and the pattern therefore has no underlying
        /// colour.
        /// </summary>
        private static ColorSpaceDetails? GetUnderlyingColorSpace(PatternColorSpaceDetails patternColorSpace,
            PatternColor patternColor, double[]? operands)
        {
            if (patternColor is not TilingPatternColor { PaintType: PatternPaintType.Uncoloured })
            {
                return null;
            }

            var underlying = patternColorSpace.UnderlyingColourSpace;

            if (underlying is null or UnsupportedColorSpaceDetails or PatternColorSpaceDetails)
            {
                return null;
            }

            if (operands is not null && operands.Length != underlying.NumberOfColorComponents)
            {
                return null;
            }

            return underlying;
        }

        /// <summary>
        /// The converted colour. For a Pattern colour this is the pattern itself; see
        /// <see cref="UnderlyingColor"/> for the colour an uncoloured tiling pattern paints in.
        /// </summary>
        public IColor? Color => patternColor ?? color;

        /// <summary>
        /// The colour an uncoloured tiling pattern paints its content in. <see langword="null"/> unless a
        /// Pattern colour was selected <i>and</i> its colour space declared a usable underlying space.
        /// </summary>
        public IColor? UnderlyingColor => patternColor is null ? null : color;

        private static IColor? Convert(ColorSpaceDetails colorSpace, double[]? operands)
            => operands is null
                ? colorSpace.GetInitializeColor()
                : colorSpace.GetColor(operands);

        /// <summary>
        /// <see cref="Convert"/>, but answering <see langword="null"/> instead of throwing when the
        /// underlying space cannot turn the operands into a colour.
        /// </summary>
        private static IColor? TryConvert(ColorSpaceDetails colorSpace, double[]? operands)
        {
            try
            {
                return Convert(colorSpace, operands);
            }
            catch
            {
                return null;
            }
        }
    }
}
