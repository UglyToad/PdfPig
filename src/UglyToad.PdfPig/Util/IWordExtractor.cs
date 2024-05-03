namespace UglyToad.PdfPig.Util
{
    using Content;

    /// <summary>
    /// An approach used to generate words from a set of letters.
    /// </summary>
    public interface IWordExtractor
    {
        /// <summary>
        /// Generate words from the input set of letters.
        /// </summary>
        /// <param name="letters">The letters to generate words for.</param>
        /// <returns>An enumerable of words from this approach.</returns>
        IEnumerable<Word> GetWords(IReadOnlyList<Letter> letters);
    }
}
