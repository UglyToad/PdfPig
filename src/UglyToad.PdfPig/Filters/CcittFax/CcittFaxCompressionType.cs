namespace UglyToad.PdfPig.Filters.CcittFax
{
    /// <summary>
    /// Specifies the internal CCITT row format selected from PDF decode parameters.
    /// K selects one-dimensional, mixed Group 3 or Group 4 decoding. For K = 0, EndOfLine
    /// or header detection selects whether rows use end-of-line (EOL) synchronization.
    /// </summary>
    internal enum CcittFaxCompressionType : byte
    {
        /// <summary>
        /// Modified Huffman (MH), T.4: one-dimensional run codes without per-row EOL synchronization.
        /// </summary>
        ModifiedHuffman,
        /// <summary>
        /// Modified Huffman (MH), Group 3 (T.4): one-dimensional rows with EOL synchronization.
        /// </summary>
        Group3_1D,
        /// <summary>
        /// Modified READ (MR), Group 3 (T.4): each row starts with EOL and a tag selecting 1D or 2D decoding.
        /// </summary>
        Group3_2D,
        /// <summary>
        /// Modified Modified READ (MMR), Group 4 (T.6): two-dimensional rows without per-row EOL synchronization.
        /// </summary>
        Group4_2D
    }
}
