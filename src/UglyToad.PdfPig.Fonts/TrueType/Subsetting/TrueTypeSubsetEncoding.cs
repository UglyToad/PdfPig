namespace UglyToad.PdfPig.Fonts.TrueType.Subsetting
{
    using System.Collections.Generic;

    /// <summary>
    /// A new encoding to create for the subsetted TrueType file.
    /// </summary>
    public class TrueTypeSubsetEncoding
    {
        /// <summary>
        /// The characters to include in the subset in order where index is the character code.
        /// </summary>
        public IReadOnlyList<char> Characters { get; }

        /// <summary>
        /// Create a new <see cref="TrueTypeSubsetEncoding"/>.
        /// </summary>
        public TrueTypeSubsetEncoding(IReadOnlyList<char> characters)
        {
            Characters = characters;
        }
    }
}