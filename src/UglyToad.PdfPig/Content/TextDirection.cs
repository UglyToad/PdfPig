namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// Direction of the text.
    /// </summary>
    public enum TextDirection : byte
    {
        /// <summary>
        /// Other text direction.
        /// </summary>
        Other = 0,

        /// <summary>
        /// Usual text direction (Left to Right).
        /// </summary>
        Horizontal = 1,

        /// <summary>
        /// Horizontal text, upside down.
        /// </summary>
        Rotate180 = 2,

        /// <summary>
        /// Rotated text going down.
        /// </summary>
        Rotate90 = 3,

        /// <summary>
        /// Rotated text going up.
        /// </summary>
        Rotate270 = 4
    }
}
