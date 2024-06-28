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
        /// Gets the blocks in reading order using rendering order (TextSequence) 
        /// <para>If blocks are of type <see cref="ILettersBlock"/> it will also set the <see cref="TextBlock.ReadingOrder"/>.</para>
        /// </summary>
        /// <param name="blocks">The <see cref="IBoundingBox"/>s, to order.</param>
        /// <returns>If type is <see cref="ILettersBlock"/> the blocks ordered according to rending order. Otherwise the list unchanged.</returns>
        public IEnumerable<TBlock> Get<TBlock>(IEnumerable<TBlock> blocks)
             where TBlock : IBoundingBox
        {
            // Ordered by is a stable sort: if the keys of two elements are equal, the order of the elements is preserved 
            var ordered = blocks.OrderBy(b => GetAverageTextSequenceOrDefaultToZero(b));

            if (typeof(TBlock) == typeof(TextBlock))
            {
                return SetReadingOrder(blocks);
            }

            return blocks;
        }

        private double GetAverageTextSequenceOrDefaultToZero<TBlock>(TBlock block) 
            where TBlock : IBoundingBox
        {
            if (block is ILettersBlock textBlock)
            {
                return textBlock.Letters.Average(x => x.TextSequence);
            }

            return 0;
        }

        private IEnumerable<TBlock> SetReadingOrder<TBlock>(IEnumerable<TBlock> blocks) 
            where TBlock : IBoundingBox
        {
            int readingOrder = 0;

            foreach (var block in blocks)
            {
                var txtBlock = block as TextBlock;
                txtBlock.SetReadingOrder(readingOrder++);
                yield return block;
            }
        }
    }
}
