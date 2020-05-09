namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// Text orientation.
    /// </summary>
    public enum TextOrientation : byte
    {
        /// <summary>
        /// Other text orientation.
        /// <para>Not axis aligned.</para>
        /// </summary>
        Other = 0,

        /// <summary>
        /// Usual text orientation.
        /// <para>Axis aligned.</para>
        /// </summary>
        Horizontal = 1,

        /// <summary>
        /// Horizontal text, upside down.
        /// <para>Axis aligned.</para>
        /// </summary>
        Rotate180 = 2,

        /// <summary>
        /// Rotated text going down.
        /// <para>Axis aligned.</para>
        /// </summary>
        Rotate90 = 3,

        /// <summary>
        /// Rotated text going up.
        /// <para>Axis aligned.</para>
        /// </summary>
        Rotate270 = 4
    }
}
