namespace UglyToad.PdfPig.Filters
{
    /// <summary>
    /// Specifies the compression type to use with <see cref="T:UglyToad.PdfPig.Filters.CcittFaxDecoderStream" />.
    /// </summary>
    internal enum CcittFaxCompressionType
    {
        /// <summary>
        /// Modified Huffman (MH) - Group 3 variation (T2)
        /// </summary>
        ModifiedHuffman,
        /// <summary>
        /// Modified Huffman (MH) - Group 3 (T4)
        /// </summary>
        Group3_1D,
        /// <summary>
        /// Modified Read (MR) - Group 3 (T4)
        /// </summary>
        Group3_2D,
        /// <summary>
        /// Modified Modified Read (MMR) - Group 4 (T6)
        /// </summary>
        Group4_2D
    }
}
