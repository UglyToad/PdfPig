namespace UglyToad.PdfPig.Graphics
{
    using Colors;
    using Colors.Icc;
    using Core;
    using Tokens;

    /// <summary>
    /// A current colour, kept as the operands it was selected with so that it can be reconverted if the
    /// rendering intent changes before anything is painted with it.
    /// </summary>
    internal readonly struct PdfColorInfo
    {
        /// <summary>
        /// The colour space <see cref="operands"/> belong to, or <see langword="null"/> when
        /// <see cref="color"/> was handed over ready-made and there is nothing to reconvert from.
        /// <para>
        /// For a Pattern colour this is the <i>underlying</i> colour space of an uncoloured tiling pattern
        /// (8.7.3.3) rather than the Pattern space itself, which cannot convert anything.
        /// </para>
        /// </summary>
        private readonly ColorSpaceDetails? colorSpace;

        /// <summary>
        /// The selected component values, or <see langword="null"/> for the colour space's initial colour,
        /// which <see cref="ColorSpaceDetails.GetInitializeColor(RenderingIntent)"/> derives itself and no
        /// operand array stands behind.
        /// </summary>
        private readonly double[]? operands;

        /// <summary>
        /// What <see cref="operands"/> convert to. For a Pattern colour this is the <i>underlying</i> colour
        /// an uncoloured tiling pattern paints with, and <see cref="patternColor"/> is the colour itself.
        /// </summary>
        private readonly IColor? color;

        /// <summary>
        /// The Pattern colour, when one was selected; <see langword="null"/> otherwise. A pattern is chosen
        /// by name rather than by component values, so it never converts and never varies by intent, but
        /// the operands that may accompany the name still do, hence the two fields.
        /// </summary>
        private readonly IColor? patternColor;

        /// <summary>
        /// The output intent profile <see cref="color"/> was managed through, or <see langword="null"/> when
        /// it was not managed (14.11.5 / 8.6.5.7). Retained because the profile resolves a <i>different</i>
        /// transform per <see cref="RenderingIntent"/>, so a managed colour has to be re-managed when the
        /// intent moves - even for a device colour space, whose own conversion cannot vary.
        /// </summary>
        private readonly IIccProfile? outputIntentProfile;

        private readonly RenderingIntent intent;

        private PdfColorInfo(ColorSpaceDetails? colorSpace, double[]? operands, IColor? color,
            IColor? patternColor, RenderingIntent intent, IIccProfile? outputIntentProfile = null)
        {
            this.colorSpace = colorSpace;
            this.operands = operands;
            this.color = color;
            this.patternColor = patternColor;
            this.intent = intent;
            this.outputIntentProfile = outputIntentProfile;
        }

        /// <summary>
        /// A colour with no operands behind it, which therefore stands exactly as given whatever the intent
        /// does afterward. This is what a consumer handing over an already-converted colour stores.
        /// </summary>
        public static PdfColorInfo Fixed(IColor? color)
            => new(null, null, color, null, RenderingIntent.RelativeColorimetric);

        /// <summary>
        /// A colour selected in <paramref name="colorSpace"/>, converted now under <paramref name="intent"/>
        /// and reconvertible later under another.
        /// <para>
        /// If <see cref="ColorSpaceDetails.RenderingIntentAffectsOutput"/> is  <see langword="false"/>, <see cref="Fixed"/> is used.
        /// You should check beforehand if the operands array allocation is required.
        /// </para>
        /// </summary>
        /// <param name="colorSpace">The colour space the operands belong to.</param>
        /// <param name="operands">The selected component values, or <see langword="null"/> for the colour space's initial colour.
        /// Stored by reference, the caller must not mutate it afterward.</param>
        /// <param name="intent">The intent in force when the colour was selected.</param>
        /// <param name="outputIntentProfile">
        /// (Optional) The output intent profile to colour-manage the converted colour through, when the
        /// document declares one and the configured <see cref="IIccProfileService"/> opted in. Managing a
        /// colour makes it vary by intent whatever its colour space says, so a managed colour keeps its
        /// operands however <see cref="ColorSpaceDetails.RenderingIntentAffectsOutput"/> answered.
        /// </param>
        public static PdfColorInfo FromOperands(ColorSpaceDetails colorSpace, double[]? operands,
            RenderingIntent intent, IIccProfile? outputIntentProfile = null)
        {
            var color = Convert(colorSpace, operands, intent);

            if (outputIntentProfile is not null &&
                TryManage(colorSpace, color, outputIntentProfile, intent, out var managed))
            {
                return new(colorSpace, operands, managed, null, intent, outputIntentProfile);
            }

            // Not managed - either no output intent applies, or this colour space is not one the profile can
            // express - so the colour space's own answer decides whether anything is worth retaining.
            return colorSpace.RenderingIntentAffectsOutput
                ? new(colorSpace, operands, color, null, intent)
                : Fixed(color);
        }

        /// <summary>
        /// Convert <paramref name="color"/> through the output intent profile, reporting whether it applied.
        /// </summary>
        private static bool TryManage(ColorSpaceDetails colorSpace, IColor? color, IIccProfile profile,
            RenderingIntent intent, out IColor? managed)
            => OutputIntentColorManagement.TryConvert(color, GetEffectiveDeviceType(colorSpace), profile, intent, out managed);

        /// <summary>
        /// A Pattern colour selected by <c>SCN</c>/<c>scn</c>. The pattern itself is fixed (it comes from a
        /// name, not from component values) but an <b>uncoloured</b> tiling pattern (<c>/PaintType 2</c>)
        /// also carries the colour its content is painted in, as operands in the <i>underlying</i> colour
        /// space that the Pattern space was declared with (8.7.3.3). Those operands are kept here, converted
        /// like any other colour and reconvertible under a later intent, and read back through
        /// <see cref="UnderlyingColor"/>.
        /// </summary>
        /// <param name="patternColorSpace">The Pattern colour space the name is resolved against.</param>
        /// <param name="patternName">The name of an entry in the <c>/Pattern</c> subdictionary of the current resource dictionary.</param>
        /// <param name="operands">The component values accompanying the name, in the underlying colour space.
        /// Empty or <see langword="null"/> (normal case for a coloured tiling pattern or a shading
        /// pattern) means there is no underlying colour to select. Stored by reference, the caller must not mutate it afterward.
        /// </param>
        /// <param name="intent">The intent in force when the colour was selected.</param>
        /// <param name="outputIntentProfile">
        /// (Optional) The output intent profile to colour-manage the <i>underlying</i> colour through, as
        /// <see cref="FromOperands"/> does for an ordinary colour. The pattern itself is never managed - it
        /// is a name, and the colours its content stream paints with are managed when that stream is
        /// processed - but the colour an uncoloured tiling pattern is painted in is an ordinary device
        /// colour selected in an ordinary device colour space, so leaving it unmanaged would render it
        /// differently from the very same operands written outside a pattern.
        /// </param>
        public static PdfColorInfo ForPattern(PatternColorSpaceDetails patternColorSpace, NameToken patternName,
            double[]? operands, RenderingIntent intent, IIccProfile? outputIntentProfile = null)
        {
            // Normalise "no operands" to null so that the underlying space derives its own initial colour
            // rather than being asked to convert an empty component list.
            if (operands is { Length: 0 })
            {
                operands = null;
            }

            var patternColor = patternColorSpace.GetColor(patternName);
            var underlyingColorSpace = GetUnderlyingColorSpace(patternColorSpace, patternColor, operands);

            if (underlyingColorSpace is null)
            {
                // No underlying colour at all - a coloured tiling pattern or a shading pattern - so there is
                // nothing for an output intent to manage and nothing to reconvert on a later intent either.
                return new PdfColorInfo(null, operands, null, patternColor, intent);
            }

            var underlyingColor = Convert(underlyingColorSpace, operands, intent);

            // Retained only when it applied, as on FromOperands: the field means "the profile this colour
            // was managed through", so a colour space the profile cannot express keeps null.
            if (outputIntentProfile is not null &&
                TryManage(underlyingColorSpace, underlyingColor, outputIntentProfile, intent, out var managed))
            {
                return new PdfColorInfo(underlyingColorSpace, operands, managed, patternColor, intent,
                    outputIntentProfile);
            }

            return new PdfColorInfo(underlyingColorSpace, operands, underlyingColor, patternColor, intent);
        }

        private static ColorSpace GetEffectiveDeviceType(ColorSpaceDetails colorSpace)
            => colorSpace.Type is ColorSpace.Separation or ColorSpace.DeviceN
                ? colorSpace.BaseType
                : colorSpace.Type;

        /// <summary>
        /// The underlying colour space to convert <paramref name="operands"/> through, or
        /// <see langword="null"/> when there is no usable one and the pattern therefore has no underlying
        /// colour.
        /// </summary>
        private static ColorSpaceDetails? GetUnderlyingColorSpace(PatternColorSpaceDetails patternColorSpace,
            PatternColor patternColor, double[]? operands)
        {
            // Only an uncoloured tiling pattern (/PaintType 2) paints in a colour supplied from outside it;
            // a coloured tiling pattern and a shading pattern carry their own, so there is nothing for an
            // underlying colour to mean. Both are normally selected from a bare /Pattern space, which the
            // check below would settle - but the colour space is declared in the resource dictionary while
            // the pattern is chosen per SCN/scn, so a space with an underlying entry can be left in force
            // over one. The pattern is the one that knows which it is.
            if (patternColor is not TilingPatternColor { PaintType: PatternPaintType.Uncoloured })
            {
                return null;
            }

            var underlying = patternColorSpace.UnderlyingColourSpace;

            // An uncoloured tiling pattern still needs a space to convert its operands through. A bare
            // /Pattern declares none - malformed for /PaintType 2, but nothing here can repair it. The other
            // two cannot convert: Unsupported by definition, and a nested Pattern - which 8.7.3.3 forbids -
            // throws from every member.
            if (underlying is null or UnsupportedColorSpaceDetails or PatternColorSpaceDetails)
            {
                return null;
            }

            // Every ColorSpaceDetails.GetColor throws on a component count it did not expect. Elsewhere, that
            // is the right answer, because the operand count comes straight from the operator; here it comes
            // from a /Pattern array that may disagree with the operator, and this conversion now happens
            // while the content stream is being processed rather than when the pattern is painted. So a
            // mismatch yields no underlying colour instead of taking down every consumer of the page.
            if (operands is not null && operands.Length != underlying.NumberOfColorComponents)
            {
                return null;
            }

            return underlying;
        }

        /// <summary>
        /// The converted colour, valid for the intent this was last resolved under. For a Pattern colour
        /// this is the pattern itself; see <see cref="UnderlyingColor"/> for the colour an uncoloured tiling
        /// pattern paints in.
        /// </summary>
        public IColor? Color => patternColor ?? color;

        /// <summary>
        /// The colour an uncoloured tiling pattern paints its content in, converted for the intent this was
        /// last resolved under. <see langword="null"/> unless a Pattern colour was selected <i>and</i> its
        /// colour space declared a usable underlying space.
        /// <para>As a result, <see langword="null"/> for every ordinary colour, for a coloured tiling pattern and for a shading pattern.</para>
        /// </summary>
        public IColor? UnderlyingColor => patternColor is null ? null : color;

        /// <summary>
        /// This colour converted for <paramref name="currentIntent"/>, or itself when it already is.
        /// <para>
        /// The caller is expected to store the result back, so that a page painted entirely under a changed
        /// intent converts once rather than once per letter.
        /// </para>
        /// </summary>
        public PdfColorInfo Resolved(RenderingIntent currentIntent)
        {
            if (colorSpace is null || currentIntent == intent)
            {
                return this;
            }

            var converted = Convert(colorSpace, operands, currentIntent);

            // Re-run the output intent under the new intent too: the profile resolves its transform per
            // intent, so re-converting the operands without re-managing them would answer with the old
            // device's colour under the new intent.
            if (outputIntentProfile is not null &&
                TryManage(colorSpace, converted, outputIntentProfile, currentIntent, out var managed))
            {
                converted = managed;
            }

            return new PdfColorInfo(colorSpace, operands, converted, patternColor, currentIntent,
                outputIntentProfile);
        }

        private static IColor? Convert(ColorSpaceDetails colorSpace, double[]? operands, RenderingIntent intent)
            => operands is null
                ? colorSpace.GetInitializeColor(intent)
                : colorSpace.GetColor(operands, intent);
    }
}
