// ReSharper disable RedundantDefaultMemberInitializer
namespace UglyToad.PdfPig.Graphics
{
    using Colors;
    using Core;
    using PdfPig.Core;

    /// <summary>
    /// The state of the current graphics control parameters set by operations in the content stream.
    /// </summary>
    /// <remarks>
    /// Initialized per page.
    /// </remarks>
    public class CurrentGraphicsState : IDeepCloneable<CurrentGraphicsState>
    {
        /// <summary>
        /// The <see cref="CurrentFontState"/> for this graphics state.
        /// </summary>
        public CurrentFontState FontState { get; set; } = new CurrentFontState();

        /// <summary>
        /// Thickness in user space units of path to be stroked.
        /// </summary>
        public decimal LineWidth { get; set; } = 1;

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
        public decimal MiterLimit { get; set; } = 10;

        /// <summary>
        /// The pattern to be used for stroked lines.
        /// </summary>
        public LineDashPattern LineDashPattern { get; set; } = LineDashPattern.Solid;

        /// <summary>
        /// The rendering intent to use when converting CIE-based colors to device colors.
        /// </summary>
        public RenderingIntent RenderingIntent { get; set; } = RenderingIntent.RelativeColorimetric;

        /// <summary>
        /// Should a correction for rasterization effects be applied?
        /// </summary>
        public bool StrokeAdjustment { get; set; } = false;

        /// <summary>
        /// Opacity value to be used for transparent imaging.
        /// </summary>
        public decimal AlphaConstant { get; set; } = 1;

        /// <summary>
        /// Should soft mask and alpha constant values be interpreted as shape (<see langword="true"/>) or opacity (<see langword="false"/>) values?
        /// </summary>
        public bool AlphaSource { get; set; } = false;

        /// <summary>
        /// Maps positions from user coordinates to device coordinates.
        /// </summary>
        public TransformationMatrix CurrentTransformationMatrix { get; set; } = TransformationMatrix.Identity;

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
        public decimal OverprintMode { get; set; }

        /// <summary>
        /// The precision for rendering curves, smaller numbers give smoother curves.
        /// </summary>
        public decimal Flatness { get; set; } = 1;

        /// <summary>
        /// The precision for rendering color gradients on the output device.
        /// </summary>
        public decimal Smoothness { get; set; } = 0;

        /// <summary>
        /// The current active stroking color for paths.
        /// </summary>
        public IColor CurrentStrokingColor { get; set; }

        /// <summary>
        /// The current active non-stroking color for text and fill.
        /// </summary>
        public IColor CurrentNonStrokingColor { get; set; }

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
                AlphaConstant = AlphaConstant,
                AlphaSource = AlphaSource,
                NonStrokingOverprint = NonStrokingOverprint,
                OverprintMode = OverprintMode,
                Smoothness = Smoothness,
                StrokeAdjustment = StrokeAdjustment,
                CurrentStrokingColor = CurrentStrokingColor,
                CurrentNonStrokingColor = CurrentNonStrokingColor
            };
        }
    }
}
