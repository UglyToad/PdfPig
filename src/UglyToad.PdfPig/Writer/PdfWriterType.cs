namespace UglyToad.PdfPig.Writer
{
    /// <summary>
    /// Type of pdf writer to use.
    /// </summary>
    public enum PdfWriterType
    {
        /// <summary>
        /// Default output writer
        /// </summary>
        Default,
        /// <summary>
        /// De-duplicates objects while writing but requires keeping in memory reference.
        /// </summary>
        ObjectInMemoryDedup
    }
}