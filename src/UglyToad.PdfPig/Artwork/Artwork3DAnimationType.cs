namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// How keyframe animations in 3D artwork are driven (ISO 32000-2, Table 313).
    /// </summary>
    public enum Artwork3DAnimationType : byte
    {
        /// <summary>
        /// Keyframe animations are not driven by the viewer (the default).
        /// </summary>
        None = 0,

        /// <summary>
        /// Keyframe animations are driven linearly from beginning to end.
        /// </summary>
        Linear = 1,

        /// <summary>
        /// Keyframe animations oscillate along their time range.
        /// </summary>
        Oscillating = 2
    }
}
