namespace UglyToad.PdfPig.Filters.CcittFax
{
    /// <summary>Identifies the CCITT row framing and coding family selected from PDF parameters.</summary>
    /// <remarks>
    /// K less than zero selects Group 4; positive K selects mixed Group 3. For K equal to zero,
    /// EndOfLine or header detection selects synchronized Group 3 versus Modified Huffman runs
    /// without per-row EOL.
    /// </remarks>
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
