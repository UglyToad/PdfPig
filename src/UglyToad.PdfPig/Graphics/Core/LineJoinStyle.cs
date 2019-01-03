namespace UglyToad.PdfPig.Graphics.Core
{
    /// <summary>
    /// The line join style specifies the shape to be used at the corners of paths that are stroked.
    /// </summary>
    public enum LineJoinStyle
    {
        /// <summary>
        /// The outer edges of the strokes for the two segments are extended until they meet at an angle, as in a picture frame. 
        /// If the segments meet at too sharp an angle (as defined by the miter limit parameter), a bevel join is used instead. 
        /// </summary>
        Miter = 0,
        /// <summary>
        /// An arc of a circle with a diameter equal to the line width is drawn around the point where the two segments meet, 
        /// connecting the outer edges of the strokes for the two segments
        /// </summary>
        Round = 1,
        /// <summary>
        ///  The two segments are finished with butt caps and the resulting notch beyond the ends of the segments is filled with a triangle. 
        /// </summary>
        Bevel = 2
    }
}