namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// Orientation of the text.
    /// </summary>
    public enum TextOrientation : byte
    {
        /// <summary>
        /// Other text orientation.
        /// </summary>
        Other = 0,

        /// <summary>
        /// Usual text orientation (Left to Right).
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
