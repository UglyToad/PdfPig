namespace UglyToad.PdfPig.Filters
{
    /// <summary>
    /// Specifies the compression type to use with <see cref="T:UglyToad.PdfPig.Filters.CcittFaxDecoderStream" />.
    /// </summary>
    internal enum CcittFaxCompressionType
    {
        /// <summary>
        /// Modified Huffman - Group 3 (T4)
        /// </summary>
        ModifiedHuffman,
        /// <summary>
        /// Modified Read - Group 3 (optional T4)
        /// </summary>
        T4,
        /// <summary>
        /// Modified Modified Read - Group 4 (T6)
        /// </summary>
        T6
    }
}
