namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;

    /// <summary>
    /// Helper functions for reading ordering
    /// </summary>
    public static class ReadingOrderHelper
    {
        private static readonly HorizontalReadingOrderDetector WordsInLineReadingOrder = new HorizontalReadingOrderDetector();
        private static readonly VerticalReadingOrderDetector LinesReadingOrder = new VerticalReadingOrderDetector();

        /// <summary>
        /// Order words by reading order in a line.
        /// <para>Assumes LtR and accounts for rotation.</para>
        /// </summary>
        /// <param name="words"></param>
        public static List<Word> OrderByReadingOrder(this IEnumerable<Word> words)
        {
            return WordsInLineReadingOrder.Get(words).ToList();
        }

        /// <summary>
        /// Order lines by reading order in a block.
        /// <para>Assumes TtB and accounts for rotation.</para>
        /// </summary>
        /// <param name="lines"></param>
        public static IReadOnlyList<TextLine> OrderByReadingOrder(this IEnumerable<TextLine> lines)
        {
            return LinesReadingOrder.Get(lines).ToList();
        }

        /// <summary>
        /// Gets the text orientation of a group of blocks
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        public static TextOrientation Orientation(this IEnumerable<ILettersBlock> words)
        {
            var textOrientation = words.First().TextOrientation;
            if (textOrientation == TextOrientation.Other)
            {
                return TextOrientation.Other;
            }

            return words.Any(x => x.TextOrientation != textOrientation) ? TextOrientation.Other : textOrientation;
        }

        /// <summary>
        /// Gets the blocks in vertical Order
        /// </summary>
        /// <typeparam name="TBlock"></typeparam>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public static IEnumerable<TBlock> SimpleVerticalOrder<TBlock>(this IEnumerable<TBlock> blocks)
        where TBlock : IBoundingBox
        {
            return blocks.OrderByDescending(b => b.BoundingBox.BottomLeft.Y);
        }

        /// <summary>
        /// Gets the blocks in horizontal Order
        /// </summary>
        public static IEnumerable<TBlock> SimpleHorizontalOrder<TBlock>(this IEnumerable<TBlock> blocks)
            where TBlock : IBoundingBox
        {
            return blocks.OrderBy(b => b.BoundingBox.BottomLeft.X);
        }


        /// <summary>
        /// Gets the blocks ordered in the horizontal direction of the rotation
        /// For example given a line with words rotated in the same direction, this will order
        /// along a horizontal (nominal x) direction in relation to the rotation
        /// </summary>
        /// <typeparam name="TBlock"></typeparam>
        /// <param name="blocks"></param>
        /// <returns>Blocks ordered in the Horizontal direction of the rotation</returns>
        public static IEnumerable<TBlock> AngledHorizontalOrderDector<TBlock>(this IEnumerable<TBlock> blocks)
            where TBlock : IBoundingBox
        {
            // We consider the blocks roughly have the same rotation.
            var avgAngle = blocks.Average(w => w.BoundingBox.Rotation);
            if (double.IsNaN(avgAngle))
            {
                throw new NotFiniteNumberException("OrderByReadingOrder: NaN bounding box rotation found when ordering words.", avgAngle);
            }

            if (0 < avgAngle && avgAngle <= 90)
            {
                // quadrant 1, 0 < θ < π/2
                return blocks.OrderBy(w => w.BoundingBox.BottomLeft.X).ThenBy(w => w.BoundingBox.BottomLeft.Y).ToList();
            }
            else if (90 < avgAngle && avgAngle <= 180)
            {
                // quadrant 2, π/2 < θ ≤ π
                return blocks.OrderByDescending(w => w.BoundingBox.BottomLeft.X).ThenBy(w => w.BoundingBox.BottomLeft.Y).ToList();
            }
            else if (-180 < avgAngle && avgAngle <= -90)
            {
                // quadrant 3, -π < θ < -π/2
                return blocks.OrderByDescending(w => w.BoundingBox.BottomLeft.X).ThenByDescending(w => w.BoundingBox.BottomLeft.Y).ToList();
            }
            else if (-90 < avgAngle && avgAngle <= 0)
            {
                // quadrant 4, -π/2 < θ < 0
                return blocks.OrderBy(w => w.BoundingBox.BottomLeft.X).ThenByDescending(w => w.BoundingBox.BottomLeft.Y).ToList();
            }
            else
            {
                throw new ArgumentException("OrderByReadingOrder: unknown bounding box rotation found when ordering words.", nameof(avgAngle));
            }
        }

        /// <summary>
        /// Gets the blocks ordered in the vertical direction of the rotation
        /// For example given a list of lines rotated in the same direction, this will order the lines
        /// along a vertical (nominal y) direction in relation to the rotation
        /// </summary>
        /// <typeparam name="TBlock"></typeparam>
        /// <param name="blocks"></param>
        /// <returns>Blocks ordered in the Vertical direction of the rotation</returns>
        public static IEnumerable<TBlock> AngledVerticalOrder<TBlock>(this IEnumerable<TBlock> blocks)
            where TBlock : IBoundingBox
        {
            // We consider the blocks roughly have the same rotation.
            var avgAngle = blocks.Average(w => w.BoundingBox.Rotation);
            if (double.IsNaN(avgAngle))
            {
                throw new NotFiniteNumberException("OrderByReadingOrder: NaN bounding box rotation found when ordering lines.", avgAngle);
            }

            if (0 < avgAngle && avgAngle <= 90)
            {
                // quadrant 1, 0 < θ < π/2
                return blocks.OrderByDescending(w => w.BoundingBox.BottomLeft.Y).ThenBy(w => w.BoundingBox.BottomLeft.X).ToList();
            }
            else if (90 < avgAngle && avgAngle <= 180)
            {
                // quadrant 2, π/2 < θ ≤ π
                return blocks.OrderBy(w => w.BoundingBox.BottomLeft.X).ThenBy(w => w.BoundingBox.BottomLeft.Y).ToList();
            }
            else if (-180 < avgAngle && avgAngle <= -90)
            {
                // quadrant 3, -π < θ < -π/2
                return blocks.OrderBy(w => w.BoundingBox.BottomLeft.Y).ThenByDescending(w => w.BoundingBox.BottomLeft.X).ToList();
            }
            else if (-90 < avgAngle && avgAngle <= 0)
            {
                // quadrant 4, -π/2 < θ < 0
                return blocks.OrderByDescending(w => w.BoundingBox.BottomLeft.X).ThenByDescending(w => w.BoundingBox.BottomLeft.Y).ToList();
            }
            else
            {
                throw new ArgumentException("OrderByReadingOrder: unknown bounding box rotation found when ordering lines.", nameof(avgAngle));
            }
        }
    }
}