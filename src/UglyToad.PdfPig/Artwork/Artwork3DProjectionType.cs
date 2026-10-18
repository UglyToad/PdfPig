namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The type of projection of a 3D view (ISO 32000-2, Table 316).
    /// </summary>
    public enum Artwork3DProjectionType : byte
    {
        /// <summary>
        /// Perspective projection.
        /// </summary>
        Perspective = 0,

        /// <summary>
        /// Orthographic projection.
        /// </summary>
        Orthographic = 1
    }
}
