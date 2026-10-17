namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The file format of the 3D artwork carried by a 3D stream (ISO 32000-2, Table 311).
    /// </summary>
    public enum Artwork3DFormat : byte
    {
        /// <summary>
        /// A format this library does not recognise. Consumers should leave the annotation showing its normal appearance.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Universal 3D (ECMA-363).
        /// </summary>
        U3D = 1,

        /// <summary>
        /// Product Representation Compact (ISO 14739-1).
        /// </summary>
        Prc = 2
    }
}
