namespace UglyToad.Pdf.Fonts.Cmap
{
    /// <summary>
    /// Specifies the character collection associated with the <see cref="CharacterIdentifierFont"/> (CIDFont).
    /// </summary>
    public struct CharacterIdentifierSystemInfo
    {
        /// <summary>
        /// Identifies the issuer of the character collection.
        /// </summary>
        public string Registry { get; }

        /// <summary>
        /// Uniquely identifies the character collection within the parent registry.
        /// </summary>
        public string Ordering { get; }

        /// <summary>
        /// The supplement number of the character collection.
        /// </summary>
        public int Supplement { get; }

        public CharacterIdentifierSystemInfo(string registry, string ordering, int supplement)
        {
            Registry = registry;
            Ordering = ordering;
            Supplement = supplement;
        }
    }
}