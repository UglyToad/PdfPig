namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;

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
        /// <param name="blocks">The <see cref="TextBlock"/>s to order.</param>
        /// <returns>the orignal list if type is not <see cref="TextBlock"/></returns>
        public IEnumerable<TBlock> Get<TBlock>(IEnumerable<TBlock> blocks)
             where TBlock : IBoundingBox
        {
            if (typeof(TBlock) != typeof(TextBlock))
            {
                return blocks;
            }

            return OrderByRending(blocks);
        }

        private IEnumerable<TBlock> OrderByRending<TBlock>(IEnumerable<TBlock> blocks) 
            where TBlock : IBoundingBox
        {
            int readingOrder = 0;

            foreach (var block in blocks.OrderBy(b => AvgTextSequence(b as TextBlock)))
            {
                var txtBlock = block as TextBlock;
                txtBlock.SetReadingOrder(readingOrder++);
                yield return block;
            }
        }

        private double AvgTextSequence(TextBlock textBlock)
        {
            return textBlock.TextLines.SelectMany(tl => tl.Words).SelectMany(w => w.Letters).Select(l => l.TextSequence).Average();
        }
    }
}
