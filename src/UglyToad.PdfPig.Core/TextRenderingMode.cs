namespace UglyToad.PdfPig.Core
{
    /// <summary>
    /// The text rendering mode determines whether showing text causes glyph outlines to be stroked, filled, used as a clipping boundary,
    ///  or some combination of the three.
    /// </summary>
    public enum TextRenderingMode
    {
        /// <summary>
        /// Fill text.
        /// </summary>
        /// <remarks>
        /// Corresponds to filling the entire letter region.
        /// </remarks>
        Fill = 0,
        /// <summary>
        /// Stroke text.
        /// </summary>
        /// <remarks>
        /// Corresponds to drawing the border/outline of the letter.
        /// </remarks>
        Stroke = 1,
        /// <summary>
        /// Fill then stroke text.
        /// </summary>
        FillThenStroke = 2,
        /// <summary>
        /// Neither fill nor stroke text thereby causing text to become invisible.
        /// </summary>
        Neither = 3,
        /// <summary>
        /// Fill the text and add to the clipping path.
        /// </summary>
        FillClip = 4,
        /// <summary>
        /// Stroke the text and add to the clipping path.
        /// </summary>
        StrokeClip = 5,
        /// <summary>
        /// Fill then stroke the text and then add to the clipping path.
        /// </summary>
        FillThenStrokeClip = 6,
        /// <summary>
        /// Neither fill nor stroke but add to the clipping path.
        /// </summary>
        NeitherClip = 7
    }
}
