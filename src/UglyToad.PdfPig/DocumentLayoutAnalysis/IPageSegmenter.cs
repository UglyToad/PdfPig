using System.Collections.Generic;
using UglyToad.PdfPig.Content;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// Page segmentation divides a page into areas, each consisting of a layout structure (blocks, lines, etc.).
    /// <para> See 'Performance Comparison of Six Algorithms for Page Segmentation' by Faisal Shafait, Daniel Keysers, and Thomas M. Breuel.</para>
    /// </summary>
    public interface IPageSegmenter
    {
        /// <summary>
        /// Get the text blocks.
        /// </summary>
        /// <param name="pageWords">The words to generate text blocks for.</param>
        /// <returns>A list of text blocks from this approach.</returns>
        IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> pageWords);
    }
}
