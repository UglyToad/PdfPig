namespace UglyToad.PdfPig.Filters.CcittFax
{
    /// <summary>Identifies the CCITT row coding and EOL framing used by both decoder implementations.</summary>
    /// <remarks>The PDF filter resolves K and EndOfLine into these modes. A 1D row encodes
    /// alternating color runs; a 2D row describes changes relative to the preceding row.</remarks>
    internal enum CcittFaxCompressionType : byte
    {
        /// <summary>Modified Huffman (MH): T.4 one-dimensional run codes without required row EOL synchronization.</summary>
        ModifiedHuffman,

        /// <summary>Modified Huffman (MH), Group 3 (T.4): one-dimensional rows with EOL framing.</summary>
        Group3_1D,

        /// <summary>Modified READ (MR), Group 3 (T.4): a tag selects one- or two-dimensional coding for each row.</summary>
        Group3_2D,

        /// <summary>Modified Modified READ (MMR), Group 4 (T.6): two-dimensional rows.</summary>
        Group4_2D
    }
}