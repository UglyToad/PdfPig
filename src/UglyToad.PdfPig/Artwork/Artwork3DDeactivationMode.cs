namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The circumstances under which 3D artwork is deactivated (the <c>D</c> entry, ISO 32000-2, Table 310).
    /// </summary>
    public enum Artwork3DDeactivationMode : byte
    {
        /// <summary>
        /// As soon as the page containing the annotation becomes invisible (<c>PI</c>, the default).
        /// </summary>
        PageInvisible = 0,

        /// <summary>
        /// As soon as the page is closed (<c>PC</c>).
        /// </summary>
        PageClose = 1,

        /// <summary>
        /// Only when explicitly deactivated by a script or user action (<c>XD</c>).
        /// </summary>
        Explicit = 2
    }
}
