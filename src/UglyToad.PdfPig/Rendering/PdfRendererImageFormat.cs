namespace UglyToad.PdfPig.Rendering
{
    /// <summary>
    /// The output image format of the <see cref="IPageImageRenderer"/>.
    /// </summary>
    public enum PdfRendererImageFormat : byte
    {
        /// <summary>
        /// Bitmap image format.
        /// </summary>
        Bmp,

        /// <summary>
        /// Jpeg/Jpg image format.
        /// </summary>
        Jpeg,

        /// <summary>
        /// Png image format.
        /// </summary>
        Png,

        /// <summary>
        /// Tiff image format.
        /// </summary>
        Tiff,

        /// <summary>
        /// Gif image format.
        /// </summary>
        Gif
    }
}
