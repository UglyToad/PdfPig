namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// Where activated 3D artwork is presented (the <c>Style</c> entry, ISO 32000-2, Table 310).
    /// </summary>
    public enum Artwork3DPresentationStyle : byte
    {
        /// <summary>
        /// Within the page, in the annotation's rectangle (the default).
        /// </summary>
        Embedded = 0,

        /// <summary>
        /// In a floating window.
        /// </summary>
        Windowed = 1
    }
}
