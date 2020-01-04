namespace UglyToad.PdfPig.Fonts.TrueType.Subsetting
{
    using System.Collections.Generic;

    internal class TrueTypeSubsetEncoding
    {
        public IReadOnlyList<char> Characters { get; }

        public TrueTypeSubsetEncoding(IReadOnlyList<char> characters)
        {
            Characters = characters;
        }
    }
}