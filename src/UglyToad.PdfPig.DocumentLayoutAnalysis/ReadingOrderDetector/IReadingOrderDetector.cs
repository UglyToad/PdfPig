namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Content;

    /// <summary>
    /// Reading order detector determines the page's blocks reading order.
    /// </summary>
    public interface IReadingOrderDetector
    {
        /// <summary>
        /// Gets the blocks in reading order. The results is the correctly ordered Enumerable
        /// </summary>
        /// <param name="blocks">The objects implementing <see cref="IBoundingBox"/>s to order.</param>
        IEnumerable<TBlock> Get<TBlock>(IEnumerable<TBlock> blocks) where TBlock : IBoundingBox;
    }
}
