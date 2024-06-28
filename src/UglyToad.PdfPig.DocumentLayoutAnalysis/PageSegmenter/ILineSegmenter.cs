namespace UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Content;


    /// <summary>
    /// Segments words into Lines
    /// </summary>
    public interface ILineSegmenter
    {

        /// <summary>
        /// Splits the words into lines.
        /// </summary>
        /// <param name="words"><see cref="Word"/>s to split into lines</param>
        /// <param name="wordSeparator">Default value is ' ' (space).</param>
        /// <returns></returns>
        IEnumerable<TextLine> GetLines(IEnumerable<Word> words, string wordSeparator = " ");
    }
}