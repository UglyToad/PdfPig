namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;


    /// <summary>
    /// Order blocks by reading order in a horizontal line.
    /// <para>Assumes LtR. Accounts for rotation when TBlock implements <see cref="ILettersBlock"/>.</para>
    /// </summary>
    public class HorizontalReadingOrderDetector : IReadingOrderDetector
    {
        /// <summary>
        /// Order blocks by reading order in a horizontal line.
        /// <para>Assumes LtR. Accounts for rotation when TBlock implements <see cref="ILettersBlock"/>.</para>
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
                return blocks.SimpleHorizontalOrder();
            }
        }

        /// <summary>
        /// Order words by reading order in a line.
        /// <para>Assumes LtR and accounts for rotation.</para>
        /// </summary>
        public IEnumerable<ILettersBlock> OrderByReadingOrder(IEnumerable<ILettersBlock> words)
        {
            if (words.Count() <= 1)
            {
                return words.ToList();
            }

            var textOrientation = words.Orientation();


            switch (textOrientation)
            {
                case TextOrientation.Horizontal:      
                    return words.SimpleHorizontalOrder();

                case TextOrientation.Rotate180:
                    return words.SimpleHorizontalOrder().Reverse();

                case TextOrientation.Rotate90:
                    return words.SimpleVerticalOrder();

                case TextOrientation.Rotate270:
                    return words.SimpleVerticalOrder().Reverse();

                case TextOrientation.Other:
                default:
                    return words.AngledHorizontalOrderDector();
                    
            }
        }

    }
}
