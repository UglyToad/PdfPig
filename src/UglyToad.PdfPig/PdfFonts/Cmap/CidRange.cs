namespace UglyToad.PdfPig.PdfFonts.Cmap
{
    using System;

    /// <summary>
    /// Associates the beginning and end of a range of character codes with the starting CID for the range.
    /// </summary>
    internal readonly struct CidRange
    {
        /// <summary>
        /// The beginning of the range of character codes.
        /// </summary>
        private readonly int firstCharacterCode;

        /// <summary>
        /// The end of the range of character codes.
        /// </summary>
        private readonly int lastCharacterCode;

        /// <summary>
        /// The CID associated with the beginning character code.
        /// </summary>
        private readonly int cid;

        /// <summary>
        /// Creates a new <see cref="CidRange"/> to associate a range of character codes to a range of CIDs.
        /// </summary>
        /// <param name="firstCharacterCode">The first character code in the range.</param>
        /// <param name="lastCharacterCode">The last character code in the range.</param>
        /// <param name="cid">The first CID for the range.</param>
        public CidRange(int firstCharacterCode, int lastCharacterCode, int cid)
        {
            if (lastCharacterCode < firstCharacterCode)
            {
                throw new ArgumentOutOfRangeException(nameof(lastCharacterCode), "The last character code cannot be lower than the first character code: " +
                                                                                 $"First: {firstCharacterCode}, Last: {lastCharacterCode}, CID: {cid}");
            }

            this.firstCharacterCode = firstCharacterCode;
            this.lastCharacterCode = lastCharacterCode;
            this.cid = cid;
        }

        /// <summary>
        /// Determines if this <see cref="CidRange"/> contains a mapping for the character code.
        /// </summary>
        public bool Contains(int characterCode)
        {
            return firstCharacterCode <= characterCode && characterCode <= lastCharacterCode;
        }

        /// <summary>
        /// Attempts to map the given character code to the corresponding CID in this range.
        /// </summary>
        /// <param name="characterCode">Character code</param>
        /// <param name="cidValue">The CID if found.</param>
        /// <returns><see langword="true"/> if the character code maps to a CID in this range or <see langword="false"/> if the character is out of range.</returns>
        public bool TryMap(int characterCode, out int cidValue)
        {
            cidValue = 0;

            if (Contains(characterCode))
            {
                cidValue = cid + (characterCode - firstCharacterCode);

                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $"CID {cid}: Code {firstCharacterCode} -> {lastCharacterCode}";
        }
    }
}
