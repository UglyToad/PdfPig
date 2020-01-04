namespace UglyToad.PdfPig.PdfFonts.Cmap
{
    /// <summary>
    /// Maps from a single character code to its CID.
    /// </summary>
    internal struct CidCharacterMapping
    {
        /// <summary>
        /// The character code.
        /// </summary>
        public int SourceCharacterCode { get; }

        /// <summary>
        /// The CID to map to.
        /// </summary>
        public int DestinationCid { get; }

        /// <summary>
        /// Creates a new single mapping from a character code to a CID.
        /// </summary>
        public CidCharacterMapping(int sourceCharacterCode, int destinationCid)
        {
            SourceCharacterCode = sourceCharacterCode;
            DestinationCid = destinationCid;
        }

        public override string ToString()
        {
            return $"Code {SourceCharacterCode} -> CID {DestinationCid}";
        }
    }
}
