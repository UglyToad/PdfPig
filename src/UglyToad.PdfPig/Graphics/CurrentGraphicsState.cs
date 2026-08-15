// ReSharper disable RedundantDefaultMemberInitializer
namespace UglyToad.PdfPig.Graphics
{
    using Colors;
    using Core;
    using PdfPig.Core;
    using Colors.Icc;
    using Tokens;

    /// <summary>
    /// The state of the current graphics control parameters set by operations in the content stream.
    /// </summary>
    /// <remarks>
    /// Initialized per page.
    /// </remarks>
    public class CurrentGraphicsState : IDeepCloneable<CurrentGraphicsState>
    {
        /// <summary>
        /// The current clipping path.
        /// </summary>
        public PdfPath CurrentClippingPath { get; set; }

        /// <summary>
        /// The <see cref="CurrentFontState"/> for this graphics state.
        /// </summary>
        public CurrentFontState? FontState { get; set; } = new CurrentFontState();

        /// <summary>
        /// Thickness in user space units of path to be stroked.
        /// </summary>
        public double LineWidth { get; set; } = 1;

        /// <summary>
        /// Specifies the shape of line ends for open stroked paths.
        /// </summary>
        public LineCapStyle CapStyle { get; set; } = LineCapStyle.Butt;

        /// <summary>
        /// Specifies the shape of joins between connected stroked path segments.
        /// </summary>
        public LineJoinStyle JoinStyle { get; set; } = LineJoinStyle.Miter;

        /// <summary>
        /// Maximum length of mitered line joins for paths before becoming a bevel.
        /// </summary>
        public double MiterLimit { get; set; } = 10;

        /// <summary>
        /// The pattern to be used for stroked lines.
        /// </summary>
        public LineDashPattern LineDashPattern { get; set; } = LineDashPattern.Solid;

        /// <summary>
        /// The rendering intent to use when converting CIE-based colors to device colors.
        /// </summary>
        public RenderingIntent RenderingIntent { get; set; } = RenderingIntent.RelativeColorimetric;

        /// <summary>
        /// Every output intent in effect for the content being processed (14.11.5, "Output intents"), in the
        /// order the <c>/OutputIntents</c> array wrote them: the page's own array when it has one, otherwise
        /// the document catalog's.
        /// Which entry, if any, characterises the output device is the consumer's decision.
        /// <para>
        /// <see langword="null"/> and empty both mean "no output intent is in effect" and consumers must
        /// treat them alike. The distinction is only in how they arise: empty is what a document declaring
        /// none produces, while <see langword="null"/> is how a consumer <i>suppresses</i> an intent that
        /// does exist: a renderer clears it for the duration of a soft-mask group, where device values are
        /// an alpha/luminosity computation rather than output-device colour, and restores it afterwards.
        /// </para>
        /// </summary>
        public IReadOnlyList<OutputIntent>? OutputIntents { get; set; } = null;

        /// <summary>
        /// Should a correction for rasterization effects be applied?
        /// </summary>
        public bool StrokeAdjustment { get; set; } = false;

        /// <summary>
        /// Opacity value to be used for transparent imaging.
        /// </summary>
        public double AlphaConstantStroking { get; set; } = 1;

        /// <summary>
        /// Opacity value to be used for transparent imaging.
        /// </summary>
        public double AlphaConstantNonStroking { get; set; } = 1;

        /// <summary>
        /// Should soft mask and alpha constant values be interpreted as shape
        /// (<see langword="true"/>) or opacity (<see langword="false"/>) values?
        /// </summary>
        public bool AlphaSource { get; set; } = false;

        /// <summary>
        /// A soft-mask dictionary specifying the mask shape or mask opacity values
        /// that shall be used in the transparent imaging model, or the name None if
        /// no such mask is specified.
        /// </summary>
        public SoftMask SoftMask { get; set; }

        /// <summary>
        /// Maps positions from user coordinates to device coordinates.
        /// </summary>
        public TransformationMatrix CurrentTransformationMatrix { get; set; } = TransformationMatrix.Identity;

        /// <summary>
        /// The active colorspaces for this content stream.
        /// </summary>
        public IColorSpaceContext? ColorSpaceContext { get; set; }

        private PdfColorInfo stroking;
        private PdfColorInfo nonStroking;

        /// <summary>
        /// The message on the <see cref="NotSupportedException"/> both colour setters now throw.
        /// </summary>
        private const string ColorSetterRemoved = "Setting the current colour directly is no longer supported. Use the relevant Set[...]StrokingColor() instead.";

        /// <summary>
        /// The current active stroking color for paths.
        /// </summary>
        /// <exception cref="NotSupportedException">Always, when set. See <see cref="PdfColorInfo"/>.</exception>
        public IColor CurrentStrokingColor
        {
            get
            {
                stroking = stroking.Resolved(RenderingIntent);
                return stroking.Color!;
            }

            [Obsolete(ColorSetterRemoved, error: true)]
            set => throw new NotSupportedException(ColorSetterRemoved);
        }

        /// <summary>
        /// The current active non-stroking color for text and fill.
        /// </summary>
        /// <exception cref="NotSupportedException">Always, when set. See <see cref="PdfColorInfo"/>.</exception>
        public IColor CurrentNonStrokingColor
        {
            get
            {
                nonStroking = nonStroking.Resolved(RenderingIntent);
                return nonStroking.Color!;
            }

            [Obsolete(ColorSetterRemoved, error: true)]
            set => throw new NotSupportedException(ColorSetterRemoved);
        }

        /// <summary>
        /// The colour an <b>uncoloured</b> tiling pattern selected for stroking paints its content in, or
        /// <see langword="null"/> when the current stroking colour is not such a pattern.
        /// <para>
        /// <c>SCN</c> selects a pattern by name, and for <c>/PaintType 2</c> it also supplies the colour to
        /// paint the pattern's cell in, as operands in the underlying colour space the Pattern space was
        /// declared with (8.7.3.3). <see cref="CurrentStrokingColor"/> answers the pattern; this answers
        /// that colour. Like the current colours it follows the graphics state's
        /// <see cref="RenderingIntent"/>, and it is <see langword="null"/> for a coloured tiling pattern, a
        /// shading pattern, and any non-pattern colour.
        /// </para>
        /// </summary>
        public IColor? CurrentStrokingUnderlyingColor
        {
            get
            {
                stroking = stroking.Resolved(RenderingIntent);
                return stroking.UnderlyingColor;
            }
        }

        /// <summary>
        /// The non-stroking counterpart of <see cref="CurrentStrokingUnderlyingColor"/>, selected by <c>scn</c>.
        /// </summary>
        public IColor? CurrentNonStrokingUnderlyingColor
        {
            get
            {
                nonStroking = nonStroking.Resolved(RenderingIntent);
                return nonStroking.UnderlyingColor;
            }
        }

        /// <summary>
        /// Select the stroking colour from the operands it was written with, keeping them so that the colour
        /// can be reconverted if the rendering intent changes before anything is painted (8.6.5.8).
        /// </summary>
        /// <param name="colorSpace">The colour space the operands belong to.</param>
        /// <param name="operands">
        /// The selected component values, or <see langword="null"/> for the colour space's own initial
        /// colour. Stored by reference, the caller must not mutate the array afterward.
        /// </param>
        public void SetStrokingColor(ColorSpaceDetails colorSpace, double[]? operands)
        {
            stroking = PdfColorInfo.FromOperands(colorSpace, operands, RenderingIntent);
        }

        /// <summary>
        /// Select a Pattern colour for stroking, by name, together with any operands accompanying it.
        /// <para>
        /// The pattern itself is fixed, but for an uncoloured tiling pattern the operands select the colour
        /// its content is painted in, and that colour converts and reconverts like any other - access it
        /// through <see cref="CurrentStrokingUnderlyingColor"/>.
        /// </para>
        /// </summary>
        /// <param name="patternColorSpace">The Pattern colour space the name is resolved against.</param>
        /// <param name="patternName">The name of an entry in the <c>/Pattern</c> subdictionary of the current resource dictionary.</param>
        /// <param name="operands">The component values accompanying the name, in the pattern's underlying colour space, or empty
        /// when there are none. Stored by reference, the caller must not mutate the array afterward.</param>
        public void SetStrokingPatternColor(PatternColorSpaceDetails patternColorSpace, NameToken patternName, double[]? operands)
        {
            stroking = PdfColorInfo.ForPattern(patternColorSpace, patternName, operands, RenderingIntent);
        }

        /// <summary>
        /// The non-stroking counterpart of <see cref="SetStrokingPatternColor(PatternColorSpaceDetails, NameToken, double[])"/>.
        /// </summary>
        /// <param name="patternColorSpace">The Pattern colour space the name is resolved against.</param>
        /// <param name="patternName">The name of an entry in the <c>/Pattern</c> subdictionary of the current resource dictionary.</param>
        /// <param name="operands">The component values accompanying the name, in the pattern's underlying colour space, or empty
        /// when there are none. Stored by reference, the caller must not mutate the array afterward.</param>
        public void SetNonStrokingPatternColor(PatternColorSpaceDetails patternColorSpace, NameToken patternName, double[]? operands)
        {
            nonStroking = PdfColorInfo.ForPattern(patternColorSpace, patternName, operands, RenderingIntent);
        }

        /// <summary>
        /// Record an already-converted stroking colour, with no colour space or operands behind it.
        /// <para>
        /// <b>The colour is fixed: it stands exactly as given and will not follow a later intent change</b>,
        /// because nothing is kept to reconvert it from.
        /// </para>
        /// <para>
        /// Use it only for a colour a consumer has deliberately computed for itself. A colour selected from
        /// operands belongs on <see cref="SetStrokingColor(ColorSpaceDetails, double[])"/> and a Pattern
        /// colour on <see cref="SetStrokingPatternColor(PatternColorSpaceDetails, NameToken, double[])"/>,
        /// both of which keep what they need to answer a later <c>ri</c>.
        /// </para>
        /// </summary>
        /// <param name="color">
        /// The colour, or <see langword="null"/>, which is what a Pattern colour space's initial colour is,
        /// and which <see cref="CurrentStrokingColor"/> then hands back.
        /// </param>
        public void SetStrokingColor(IColor? color)
        {
            stroking = PdfColorInfo.Fixed(color);
        }

        /// <summary>
        /// The non-stroking counterpart of <see cref="SetStrokingColor(ColorSpaceDetails, double[])"/>.
        /// </summary>
        /// <param name="colorSpace">The colour space the operands belong to.</param>
        /// <param name="operands">The selected component values, or <see langword="null"/> for the colour
        /// space's own initial colour. Stored by reference, the caller must not mutate the array afterward.</param>
        public void SetNonStrokingColor(ColorSpaceDetails colorSpace, double[]? operands)
        {
            nonStroking = PdfColorInfo.FromOperands(colorSpace, operands, RenderingIntent);
        }

        /// <summary>
        /// The non-stroking counterpart of <see cref="SetStrokingColor(UglyToad.PdfPig.Graphics.Colors.IColor?)"/>. The same caveat
        /// applies: the colour will not follow a later intent change.
        /// </summary>
        /// <param name="color">
        /// The colour, or <see langword="null"/>, which is what a Pattern colour space's initial colour is,
        /// and which <see cref="CurrentNonStrokingColor"/> then hands back.
        /// </param>
        public void SetNonStrokingColor(IColor? color)
        {
            nonStroking = PdfColorInfo.Fixed(color);
        }

        /// <summary>
        /// The current blend mode.
        /// </summary>
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;

        #region Device Dependent

        /// <summary>
        /// Should painting in a colorant set erase (<see langword="false"/>)
        /// or leave unchanged (<see langword="true"/>) areas of other colorant sets?
        /// </summary>
        public bool Overprint { get; set; } = false;

        /// <summary>
        /// As for <see cref="Overprint"/> but with non-stroking operations.
        /// </summary>
        public bool NonStrokingOverprint { get; set; } = false;

        /// <summary>
        /// In DeviceCMYK color space a value of 0 for a component will erase a component (0)
        /// or leave it unchanged (1) for overprinting.
        /// </summary>
        public double OverprintMode { get; set; }

        /// <summary>
        /// The precision for rendering curves, smaller numbers give smoother curves.
        /// </summary>
        public double Flatness { get; set; } = 1;

        /// <summary>
        /// The precision for rendering color gradients on the output device.
        /// </summary>
        public double Smoothness { get; set; } = 0;
        #endregion

        /// <inheritdoc />
        public CurrentGraphicsState DeepClone()
        {
            return new CurrentGraphicsState
            {
                FontState = FontState?.DeepClone(),
                RenderingIntent = RenderingIntent,
                LineDashPattern = LineDashPattern,
                CurrentTransformationMatrix = CurrentTransformationMatrix,
                LineWidth = LineWidth,
                JoinStyle = JoinStyle,
                Overprint = Overprint,
                CapStyle = CapStyle,
                MiterLimit = MiterLimit,
                Flatness = Flatness,
                AlphaConstantStroking = AlphaConstantStroking,
                AlphaConstantNonStroking = AlphaConstantNonStroking,
                AlphaSource = AlphaSource,
                NonStrokingOverprint = NonStrokingOverprint,
                OverprintMode = OverprintMode,
                Smoothness = Smoothness,
                StrokeAdjustment = StrokeAdjustment,
                stroking = stroking,
                nonStroking = nonStroking,
                CurrentClippingPath = CurrentClippingPath,
                ColorSpaceContext = ColorSpaceContext?.DeepClone(),
                BlendMode = BlendMode,
                SoftMask = SoftMask,
                OutputIntents = OutputIntents
            };
        }
    }
}
