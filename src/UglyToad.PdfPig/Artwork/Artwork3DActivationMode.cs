namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The circumstances under which 3D artwork is activated (the <c>A</c> entry, ISO 32000-2, Table 310).
    /// </summary>
    public enum Artwork3DActivationMode : byte
    {
        /// <summary>
        /// Only when explicitly activated by a script or user action (<c>XA</c>, the default).
        /// </summary>
        Explicit = 0,

        /// <summary>
        /// As soon as the page containing the annotation is opened (<c>PO</c>).
        /// </summary>
        PageOpen = 1,

        /// <summary>
        /// As soon as any part of the page containing the annotation becomes visible (<c>PV</c>).
        /// </summary>
        PageVisible = 2
    }
}
