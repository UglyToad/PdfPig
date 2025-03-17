namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Core;

    /// <summary>
    /// A path is made up of one or more disconnected subpaths, each comprising a sequence of connected segments.
    /// The topology of the path is unrestricted: it may be concave or convex, may contain multiple subpaths representing
    /// disjoint areas, and may intersect itself in arbitrary ways.
    /// <para>A path shall be composed of straight and curved line segments, which may connect to one another or may be disconnected.</para>
    /// </summary>
    public class PdfPath : List<PdfSubpath>
    {
        /// <summary>
        /// Rules for determining which points lie inside/outside the path.
        /// </summary>
        public FillingRule FillingRule { get; private set; }

        /// <summary>
        /// Returns true if this is a clipping path.
        /// </summary>
        public bool IsClipping { get; private set; }

        /// <summary>
        /// Returns true if the path is filled.
        /// </summary>
        public bool IsFilled { get; private set; }

        /// <summary>
        /// The fill color.
        /// </summary>
        public IColor? FillColor { get; private set; }

        /// <summary>
        /// Returns true if the path is stroked.
        /// </summary>
        public bool IsStroked { get; private set; }

        /// <summary>
        /// The stroke color.
        /// </summary>
        public IColor? StrokeColor { get; private set; }

        /// <summary>
        /// Thickness in user space units of path to be stroked.
        /// </summary>
        public double LineWidth { get; private set; }

        /// <summary>
        /// The pattern to be used for stroked lines.
        /// </summary>
        public LineDashPattern? LineDashPattern { get; private set; }

        /// <summary>
        /// The cap style to be used for stroked lines.
        /// </summary>
        public LineCapStyle LineCapStyle { get; private set; }

        /// <summary>
        /// The join style to be used for stroked lines.
        /// </summary>
        public LineJoinStyle LineJoinStyle { get; private set; }

        /// <summary>
        /// Set the clipping mode for this path and <c>IsClipping</c> to <c>true</c>.
        /// <para><c>IsFilled</c> and <c>IsStroked</c> flags will be set to <c>false</c>.</para>
        /// </summary>
        public void SetClipping(FillingRule fillingRule)
        {
            IsFilled = false;
            IsStroked = false;
            IsClipping = true;
            FillingRule = fillingRule;
        }

        /// <summary>
        /// Set the filling rule for this path and <c>IsFilled</c> to <c>true</c>.
        /// </summary>
        public void SetFilled(FillingRule fillingRule)
        {
            IsFilled = true;
            FillingRule = fillingRule;
        }

        /// <summary>
        /// Set <c>IsStroked</c> to <c>true</c>.
        /// </summary>
        public void SetStroked()
        {
            IsStroked = true;
        }

        /// <summary>
        /// Set the path stroke details, i.e. <c>LineDashPattern</c>, <c>StrokeColor</c>, <c>LineWidth</c>, <c>LineCapStyle</c> and <c>LineJoinStyle</c>.
        /// </summary>
        /// <param name="graphicsState">The current graphics state.</param>
        public void SetStrokeDetails(CurrentGraphicsState graphicsState)
        {
            LineDashPattern = graphicsState.LineDashPattern;
            StrokeColor = graphicsState.CurrentStrokingColor;
            LineWidth = graphicsState.LineWidth;
            LineCapStyle = graphicsState.CapStyle;
            LineJoinStyle = graphicsState.JoinStyle;
        }

        /// <summary>
        /// Set the path fill details, i.e. <c>FillColor</c>.
        /// </summary>
        /// <param name="graphicsState">The current graphics state.</param>
        public void SetFillDetails(CurrentGraphicsState graphicsState)
        {
            FillColor = graphicsState.CurrentNonStrokingColor;
        }

        /// <summary>
        /// Create a clone with no Subpaths.
        /// </summary>
        internal PdfPath CloneEmpty()
        {
            PdfPath newPath = new PdfPath();
            if (IsClipping)
            {
                newPath.SetClipping(FillingRule);
            }
            else
            {
                if (IsFilled)
                {
                    newPath.SetFilled(FillingRule);
                    newPath.FillColor = FillColor;
                }

                if (IsStroked)
                {
                    newPath.SetStroked();
                    newPath.LineCapStyle = LineCapStyle;
                    newPath.LineDashPattern = LineDashPattern;
                    newPath.LineJoinStyle = LineJoinStyle;
                    newPath.LineWidth = LineWidth;
                    newPath.StrokeColor = StrokeColor;
                }
            }
            return newPath;
        }

        /// <summary>
        /// Gets a <see cref="PdfRectangle"/> which entirely contains the geometry of the defined path.
        /// </summary>
        /// <returns>For paths which don't define any geometry this returns <see langword="null"/>.</returns>
        public PdfRectangle? GetBoundingRectangle()
        {
            return PdfSubpath.GetBoundingRectangle(this);
        }
    }
}
