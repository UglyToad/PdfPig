namespace UglyToad.PdfPig.Core
{
    /// <summary>
    /// Rules for determining which points lie inside/outside a path.
    /// </summary>
    public enum ClippingRule : byte
    {
        /// <summary>
        /// No rule.
        /// </summary>
        None = 0,

        /// <summary>
        /// This even-odd rule determines whether a point is inside a path by drawing a ray from that point in 
        /// any direction and simply counting the number of path segments that cross the ray, regardless of 
        /// direction. If this number is odd, the point is inside; if even, the point is outside. This yields 
        /// the same results as the nonzero winding number rule for paths with simple shapes, but produces 
        /// different results for more complex shapes.
        /// </summary>
        EvenOdd = 1,

        /// <summary>
        /// The nonzero winding number rule determines whether a given point is inside a path by conceptually
        /// drawing a ray from that point to infinity in any direction and then examining the places where a 
        /// segment of the path crosses the ray. Starting with a count of 0, the rule adds 1 each time a path 
        /// segment crosses the ray from left to right and subtracts 1 each time a segment crosses from right 
        /// to left. After counting all the crossings, if the result is 0, the point is outside the path; 
        /// otherwise, it is inside.
        /// </summary>
        NonZeroWinding = 2
    }
}
