namespace UglyToad.PdfPig.Images.Png
{
    /// <summary>
    /// Indicates the transmission order of the image data.
    /// </summary>
    public enum InterlaceMethod : byte
    {
        /// <summary>
        /// No interlace.
        /// </summary>
        None = 0,
        /// <summary>
        /// Adam7 interlace.
        /// </summary>
        Adam7 = 1
    }
}