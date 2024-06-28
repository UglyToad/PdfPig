namespace UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter
{
    using Content;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;

    /// <summary>
    /// Segments words into Lines
    /// </summary>
    public class DefaultLineSegmenter : ILineSegmenter
    {
        private readonly double tolerance;

        /// <summary>
        /// Create an instance of default line segmenter, <see cref="DefaultLineSegmenter"/>.
        /// </summary>
        public static DefaultLineSegmenter Instance { get; } = new DefaultLineSegmenter();

        /// <summary>
        /// Create an instance of DefaultLineSegmenter using tolerance
        /// </summary>
        /// <param name="tolerance">tolerance to allow minor variances when grouping into lines. 0 is the default and very strict
        /// (bounding box bottoms must match).Higher values mean more tolerance</param>
        public DefaultLineSegmenter(double tolerance = 0)
        {
            this.tolerance = Math.Abs(tolerance);
        }

        /// <summary>
        /// Splits the words into lines.
        /// </summary>
        /// <param name="words"><see cref="Word"/>s to split into lines</param>
        /// <param name="wordSeparator">Default value is ' ' (space).</param>
        /// <returns></returns>
        public IEnumerable<TextLine> GetLines(IEnumerable<Word> words, string wordSeparator = " ")
        {
            IEnumerable<TextLine> lines;
            if (tolerance == 0)
            {
                lines = words.GroupBy(x => x.BoundingBox.Bottom)
                    .Select(x => new TextLine(x.OrderByReadingOrder(), wordSeparator));
            }
            else
            {
                // We use integer rounding and the tolerance to allow minor variances in BoundingBox.Bottom to be ignored
                lines = words.GroupBy(x => (int)(x.BoundingBox.Bottom / tolerance))
                    .Select(x => new TextLine(x.OrderByReadingOrder(), wordSeparator));
            }

            return lines.OrderByReadingOrder();
        }
    }
}
