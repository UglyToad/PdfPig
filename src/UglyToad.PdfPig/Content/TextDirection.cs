namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// Direction of the text.
    /// </summary>
    public enum TextDirection
    {
        /// <summary>
        /// Other text direction.
        /// </summary>
        Other,

        /// <summary>
        /// Usual text direction (Left to Right).
        /// </summary>
        Horizontal,

        /// <summary>
        /// Horizontal text, upside down.
        /// </summary>
        Rotate180,

        /// <summary>
        /// Rotated text going down.
        /// </summary>
        Rotate90,

        /// <summary>
        /// Rotated text going up.
        /// </summary>
        Rotate270
    }
}
