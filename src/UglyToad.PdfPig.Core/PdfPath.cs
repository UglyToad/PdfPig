namespace UglyToad.PdfPig.Core
{
    using System.Collections.Generic;

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
        /// Set the clipping mode for this path.
        /// </summary>
        public void SetClipping(FillingRule fillingRule)
        {
            IsClipping = true;
            FillingRule = fillingRule;
        }
    }
}
