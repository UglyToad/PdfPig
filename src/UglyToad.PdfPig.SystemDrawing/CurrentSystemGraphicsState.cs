namespace UglyToad.PdfPig.SystemDrawing
{
    using Core;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using UglyToad.PdfPig.Graphics;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Core;

    /// <summary>
    /// The state of the current graphics control parameters set by operations in the content stream.
    /// </summary>
    /// <remarks>
    /// Initialized per page.
    /// </remarks>
    public class CurrentSystemGraphicsState : IDeepCloneable<CurrentSystemGraphicsState>
    {
        /// <summary>
        /// The current graphics state.
        /// </summary>
        public GraphicsState GraphicsState { get; set; }

        /// <summary>
        /// The current clipping path.
        /// </summary>
        public GraphicsPath CurrentClippingPath { get; set; }

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
        public LineCap LineCap { get; set; } = LineCap.Square; //LineCapStyle.Butt;

        public DashCap DashCap { get; set; } = DashCap.Flat;

        /// <summary>
        /// Specifies the shape of joins between connected stroked path segments.
        /// </summary>
        public LineJoin JoinStyle { get; set; } = LineJoin.Miter;

        /// <summary>
        /// Maximum length of mitered line joins for paths before becoming a bevel.
        /// </summary>
        public decimal MiterLimit { get; set; } = 10;

        /// <summary>
        /// The pattern's array to be used for stroked lines.
        /// </summary>
        public float[] DashPatternArray { get; set; } = null;

        /// <summary>
        /// The pattern's phase to be used for stroked lines.
        /// </summary>
        public int DashPatternPhase { get; set; } = 0;

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
        /// The current active stroking pen for paths.
        /// </summary>
        public Pen CurrentStrokingPen
        {
            get
            {
                var pen = new Pen(CurrentStrokingColor, (float)LineWidth)
                {
                    LineJoin = JoinStyle,
                    StartCap = LineCap,
                    EndCap = LineCap,
                    DashCap = DashCap,
                    DashOffset = DashPatternPhase, // to check here
                    MiterLimit = (float)MiterLimit,
                };

                if (DashPatternArray != null && DashPatternArray.Length > 0)
                {
                    pen.DashPattern = DashPatternArray;
                }

                return pen;
            }
        }

        /// <summary>
        /// The current active non-stroking color for text and fill.
        /// </summary>
        public Color CurrentNonStrokingColor { get; set; } = Color.Black;

        /// <summary>
        /// The current active non-stroking color space for text and fill.
        /// </summary>
        public ColorSpace? CurrentNonStrokingColorSpace { get; set; }

        /// <summary>
        /// The current active stroking color for paths.
        /// </summary>
        public Color CurrentStrokingColor { get; set; } = Color.Black;

        /// <summary>
        /// The current active stroking color space for paths.
        /// </summary>
        public ColorSpace? CurrentStrokingColorSpace { get; set; }
        #endregion

        /// <inheritdoc />
        public CurrentSystemGraphicsState DeepClone()
        {
            return new CurrentSystemGraphicsState
            {
                GraphicsState = GraphicsState,
                FontState = FontState?.DeepClone(),
                RenderingIntent = RenderingIntent,
                DashCap = DashCap,
                DashPatternArray = DashPatternArray,
                DashPatternPhase = DashPatternPhase,
                LineWidth = LineWidth,
                JoinStyle = JoinStyle,
                Overprint = Overprint,
                LineCap = LineCap,
                MiterLimit = MiterLimit,
                Flatness = Flatness,
                AlphaConstant = AlphaConstant,
                AlphaSource = AlphaSource,
                NonStrokingOverprint = NonStrokingOverprint,
                OverprintMode = OverprintMode,
                Smoothness = Smoothness,
                StrokeAdjustment = StrokeAdjustment,
                CurrentStrokingColor = CurrentStrokingColor,
                CurrentNonStrokingColor = CurrentNonStrokingColor,
                CurrentClippingPath = CurrentClippingPath
            };
        }
    }
}
