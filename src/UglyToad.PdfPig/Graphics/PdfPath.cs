namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Core;

    /// <summary>
    /// A path is made up of one or more disconnected subpaths, each comprising a sequence of connected segments. The topology of the path is unrestricted: it may be concave or convex, may contain multiple subpaths representing disjoint areas, and may intersect itself in arbitrary ways.
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
        /// 
        /// </summary>
        public IColor FillColor { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IColor StrokeColor { get; set; }

        /// <summary>
        /// Thickness in user space units of path to be stroked.
        /// </summary>
        public double LineWidth { get; set; } = double.NaN;

        /// <summary>
        /// The pattern to be used for stroked lines.
        /// </summary>
        public LineDashPattern? LineDashPattern { get; set; }

        /// <summary>
        /// The cap style to be used for stroked lines.
        /// </summary>
        public LineCapStyle LineCapStyle { get; set; }

        /// <summary>
        /// The join style to be used for stroked lines.
        /// </summary>
        public LineJoinStyle LineJoinStyle { get; set; }

        /// <summary>
        /// Set the clipping mode for this path.
        /// </summary>
        public void SetClipping(FillingRule fillingRule)
        {
            IsClipping = true;
            FillingRule = fillingRule;
        }
    }
}
