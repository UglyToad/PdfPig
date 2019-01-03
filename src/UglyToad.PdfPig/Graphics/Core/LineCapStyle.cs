namespace UglyToad.PdfPig.Graphics.Core
{
    /// <summary>
    /// The line cap style specifies the shape to be used at the ends of open subpaths (and dashes, if any) when they are stroked.
    /// </summary>
    public enum LineCapStyle
    {
        /// <summary>
        ///  The stroke is squared off at the endpoint of the path. There is no projection beyond the end of the path.
        /// </summary>
        Butt = 0,
        /// <summary>
        ///  A semicircular arc with a diameter equal to the line width is drawn around the endpoint and filled in. 
        /// </summary>
        Round = 1,
        /// <summary>
        ///  The stroke continues beyond the endpoint of the path for a distance equal to half the line width and is squared off. 
        /// </summary>
        ProjectingSquare = 2
    }
}