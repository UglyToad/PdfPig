namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// How the camera-to-world matrix of a 3D view is determined (the <c>MS</c> entry, ISO 32000-2, Table 315).
    /// </summary>
    public enum Artwork3DMatrixSource : byte
    {
        /// <summary>
        /// Not specified: the view defined inside the 3D artwork is used.
        /// </summary>
        None = 0,

        /// <summary>
        /// The <c>C2W</c> entry gives the matrix.
        /// </summary>
        Matrix = 1,

        /// <summary>
        /// The view node selected by the <c>U3DPath</c> entry inside the U3D artwork gives the matrix.
        /// </summary>
        U3DPath = 2
    }
}
