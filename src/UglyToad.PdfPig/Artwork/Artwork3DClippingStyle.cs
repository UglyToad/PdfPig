namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The clipping style of a 3D projection (the <c>CS</c> entry, ISO 32000-2, Table 316).
    /// </summary>
    public enum Artwork3DClippingStyle : byte
    {
        /// <summary>
        /// Near and far clipping distances are chosen by the viewer (<c>ANF</c>, the default).
        /// </summary>
        AutomaticNearFar = 0,

        /// <summary>
        /// Near and far clipping distances are given by the <c>N</c> and <c>F</c> entries (<c>XNF</c>).
        /// </summary>
        ExplicitNearFar = 1
    }
}
