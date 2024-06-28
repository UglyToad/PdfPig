namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UglyToad.PdfPig.Content;


    /// <summary>
    /// Order 'lines' by reading order
    /// <para>Assumes TtB. Accounts for rotation when TBlock implements <see cref="ILettersBlock"/>.</para>
    /// </summary>
    public class VerticalReadingOrderDetector : IReadingOrderDetector
    {
        /// <summary>
        /// Order blocks by reading order in a vertical line.
        /// <para>Assumes TtB. Accounts for rotation when TBlock implements <see cref="ILettersBlock"/>.</para>
        /// </summary>
        public IEnumerable<TBlock> Get<TBlock>(IEnumerable<TBlock> blocks) where TBlock : IBoundingBox
        {
            if (blocks.Count() <= 1)
            {
                return blocks.ToList();
            }

            if (typeof(ILettersBlock).IsAssignableFrom(typeof(TBlock)))
            {
                return OrderByReadingOrder(blocks.Cast<ILettersBlock>()).Cast<TBlock>();
            }
            else
            {
                return blocks.SimpleVerticalOrder();
            }

        }

        /// <summary>
        /// Order blocks by reading order in a vertical line.
        /// <para>Assumes TtB. Accounts for rotation</para>
        /// </summary>
        public IEnumerable<ILettersBlock> OrderByReadingOrder(IEnumerable<ILettersBlock> lines)
        {
            if (lines.Count() <= 1)
            {
                return lines.ToList();
            }

            var textOrientation = lines.Orientation();

            switch (textOrientation)
            {
                case TextOrientation.Horizontal:
                    return lines.SimpleVerticalOrder();

                case TextOrientation.Rotate180:
                    return lines.SimpleVerticalOrder().Reverse();

                case TextOrientation.Rotate90:
                    return lines.SimpleHorizontalOrder().Reverse();

                case TextOrientation.Rotate270:
                    return lines.SimpleHorizontalOrder();

                case TextOrientation.Other:
                default:
                    return lines.AngledVerticalOrder();
            }
        }

    }
}
