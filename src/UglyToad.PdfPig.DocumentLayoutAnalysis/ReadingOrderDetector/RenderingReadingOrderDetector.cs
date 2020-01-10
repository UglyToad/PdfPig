namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Algorithm that retrieve the blocks' reading order using rendering order (TextSequence).
    /// </summary>
    public class RenderingReadingOrderDetector : IReadingOrderDetector
    {
        /// <summary>
        /// Create an instance of rendering reading order detector, <see cref="RenderingReadingOrderDetector"/>.
        /// <para>This detector uses the rendering order (TextSequence).</para>
        /// </summary>
        public static RenderingReadingOrderDetector Instance { get; } = new RenderingReadingOrderDetector();

        /// <summary>
        /// Gets the blocks in reading order and sets the <see cref="TextBlock.ReadingOrder"/>.
        /// </summary>
        /// <param name="textBlocks">The <see cref="TextBlock"/>s to order.</param>
        public IEnumerable<TextBlock> Get(IReadOnlyList<TextBlock> textBlocks)
        {
            int readingOrder = 0;

            foreach (var block in textBlocks.OrderBy(b => AvgTextSequence(b)))
            {
                block.SetReadingOrder(readingOrder++);
                yield return block;
            }
        }

        private double AvgTextSequence(TextBlock textBlock)
        {
            return textBlock.TextLines.SelectMany(tl => tl.Words).SelectMany(w => w.Letters).Select(l => l.TextSequence).Average();
        }
    }
}
