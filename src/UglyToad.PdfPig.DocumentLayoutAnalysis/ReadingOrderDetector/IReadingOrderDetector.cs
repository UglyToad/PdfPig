namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System.Collections.Generic;

    /// <summary>
    /// Reading order detector determines the page's blocks reading order.
    /// <para>Note: Make sure you use <see cref="TextBlock.SetReadingOrder(int)"/> to set each <see cref="TextBlock"/> reading order when implementing <see cref="IReadingOrderDetector.Get(IReadOnlyList{TextBlock})"/>.</para>
    /// </summary>
    public interface IReadingOrderDetector
    {
        /// <summary>
        /// Gets the blocks in reading order and sets the <see cref="TextBlock.ReadingOrder"/>.
        /// </summary>
        /// <param name="textBlocks">The <see cref="TextBlock"/>s to order.</param>
        IEnumerable<TextBlock> Get(IReadOnlyList<TextBlock> textBlocks);
    }
}
