namespace UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter
{
    using Content;
    using System.Collections.Generic;

    /// <summary>
    /// Page segmentation divides a page into areas, each consisting of a layout structure (blocks, lines, etc.).
    /// <para> See 'Performance Comparison of Six Algorithms for Page Segmentation' by Faisal Shafait, Daniel Keysers, and Thomas M. Breuel.</para>
    /// </summary>
    public interface IPageSegmenter
    {
        /// <summary>
        /// Get the blocks using default options values.
        /// </summary>
        /// <param name="words">The page's words to generate text blocks for.</param>
        /// <returns>A list of text blocks from this approach.</returns>
        IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> words);

        /// <summary>
        /// Get the text blocks using options.
        /// </summary>
        /// <param name="words">The page's words to generate text blocks for.</param>
        /// <param name="options"></param>
        /// <returns>A list of text blocks from this approach.</returns>
        IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> words, DlaOptions options);
    }
}
