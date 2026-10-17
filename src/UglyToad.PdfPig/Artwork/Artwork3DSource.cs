namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The kind of annotation a piece of 3D artwork was found in.
    /// </summary>
    public enum Artwork3DSource : byte
    {
        /// <summary>
        /// A 3D annotation (ISO 32000-2, 13.6.2).
        /// </summary>
        Annotation3D = 0,

        /// <summary>
        /// A rich media annotation whose content is 3D (ISO 32000-2, 13.7).
        /// </summary>
        RichMedia = 1
    }
}
