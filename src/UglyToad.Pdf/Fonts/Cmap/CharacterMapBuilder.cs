namespace UglyToad.Pdf.Fonts.Cmap
{
    /// <summary>
    /// A mutable class used when parsing and generating a <see cref="CMap"/>.
    /// </summary>
    internal class CharacterMapBuilder
    {
        /// <summary>
        ///  Defines the character collection associated CIDFont/s for this CMap.
        /// </summary>
        public CharacterIdentifierSystemInfo CharacterIdentifierSystemInfo { get; set; }

        /// <summary>
        /// An <see langword="int"/> that determines the writing mode for any CIDFont combined with this CMap.
        /// 0: Horizontal
        /// 1: Vertical
        /// </summary>
        /// <remarks>
        /// Defined as optional.
        /// </remarks>
        public int WMode { get; set; } = 0;

        /// <summary>
        /// The PostScript name of the CMap.
        /// </summary>
        /// <remarks>
        /// Defined as required.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// Defines the version of this CIDFont file.
        /// </summary>
        /// <remarks>
        /// Defined as optional.
        /// </remarks>
        public string Version { get; set; }

        /// <summary>
        /// Defines changes to the internal structure of Character Map files
        /// or operator semantics.
        /// </summary>
        /// <remarks>
        /// Defined as required.
        /// </remarks>
        public int Type { get; set; } = -1;
    }
}
