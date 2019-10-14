using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Util;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// Default Page Segmenter. All words are included in one block.
    /// </summary>
    public class DefaultPageSegmenter : IPageSegmenter
    {
        /// <summary>
        /// Create an instance of default page segmenter, <see cref="DefaultPageSegmenter"/>.
        /// </summary>
        public static DefaultPageSegmenter Instance { get; } = new DefaultPageSegmenter();

        /// <summary>
        /// Get the blocks.
        /// </summary>
        /// <param name="pageWords">The words in the page.</param>
        public IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> pageWords)
        {
            if (pageWords.Count() == 0) return EmptyArray<TextBlock>.Instance;

            return new List<TextBlock>() { new TextBlock(new XYLeaf(pageWords).GetLines()) };
        }
    }
}
