namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;

    /// <summary>
    /// Helper functions for words and lines ordering.
    /// </summary>
    public static class ReadingOrderHelper
    {
        /// <summary>
        /// Order words by reading order in a line.
        /// <para>Assumes LtR and accounts for rotation.</para>
        /// </summary>
        /// <param name="words"></param>
        public static List<Word> OrderByReadingOrder(this IEnumerable<Word> words)
        {
            if (words.Count() <= 1)
            {
                return words.ToList();
            }

            var TextOrientation = words.First().TextOrientation;
            if (TextOrientation != TextOrientation.Other)
            {
                foreach (var word in words)
                {
                    if (word.TextOrientation != TextOrientation)
                    {
                        TextOrientation = TextOrientation.Other;
                        break;
                    }
                }
            }

            switch (TextOrientation)
            {
                case TextOrientation.Horizontal:
                    return words.OrderBy(w => w.BoundingBox.BottomLeft.X).ToList();

                case TextOrientation.Rotate180:
                    return words.OrderByDescending(w => w.BoundingBox.BottomLeft.X).ToList();

                case TextOrientation.Rotate90:
                    return words.OrderByDescending(w => w.BoundingBox.BottomLeft.Y).ToList();

                case TextOrientation.Rotate270:
                    return words.OrderBy(w => w.BoundingBox.BottomLeft.Y).ToList();

                case TextOrientation.Other:
                default:
                    // We consider the words roughly have the same rotation.
                    var avgAngle = words.Average(w => w.BoundingBox.Rotation);
                    if (double.IsNaN(avgAngle))
                    {
                        throw new NotFiniteNumberException("DocstrumBoundingBoxes: NaN bounding box rotation found when ordering words.", avgAngle);
                    }

                    if (0 < avgAngle && avgAngle <= 90)
                    {
                        // quadrant 1, 0 < θ < π/2
                        return words.OrderBy(w => w.BoundingBox.BottomLeft.X).ThenBy(w => w.BoundingBox.BottomLeft.Y).ToList();
                    }
                    else if (90 < avgAngle && avgAngle <= 180)
                    {
                        // quadrant 2, π/2 < θ ≤ π
                        return words.OrderByDescending(w => w.BoundingBox.BottomLeft.X).ThenBy(w => w.BoundingBox.BottomLeft.Y).ToList();
                    }
                    else if (-180 < avgAngle && avgAngle <= -90)
                    {
                        // quadrant 3, -π < θ < -π/2
                        return words.OrderByDescending(w => w.BoundingBox.BottomLeft.X).ThenByDescending(w => w.BoundingBox.BottomLeft.Y).ToList();
                    }
                    else if (-90 < avgAngle && avgAngle <= 0)
                    {
                        // quadrant 4, -π/2 < θ < 0
                        return words.OrderBy(w => w.BoundingBox.BottomLeft.X).ThenByDescending(w => w.BoundingBox.BottomLeft.Y).ToList();
                    }
                    else
                    {
                        throw new ArgumentException("DocstrumBoundingBoxes: unknown bounding box rotation found when ordering lines.", nameof(avgAngle));
                    }
            }
        }

        /// <summary>
        /// Order lines by reading order in a block.
        /// <para>Assumes TtB and accounts for rotation.</para>
        /// </summary>
        /// <param name="lines"></param>
        public static IReadOnlyList<TextLine> OrderByReadingOrder(this IEnumerable<TextLine> lines)
        {
            if (lines.Count() <= 1)
            {
                return lines.ToList();
            }

            var TextOrientation = lines.First().TextOrientation;
            if (TextOrientation != TextOrientation.Other)
            {
                foreach (var line in lines)
                {
                    if (line.TextOrientation != TextOrientation)
                    {
                        TextOrientation = TextOrientation.Other;
                        break;
                    }
                }
            }

            switch (TextOrientation)
            {
                case TextOrientation.Horizontal:
                    return lines.OrderByDescending(w => w.BoundingBox.BottomLeft.Y).ToList();

                case TextOrientation.Rotate180:
                    return lines.OrderBy(w => w.BoundingBox.BottomLeft.Y).ToList();

                case TextOrientation.Rotate90:
                    return lines.OrderByDescending(w => w.BoundingBox.BottomLeft.X).ToList();

                case TextOrientation.Rotate270:
                    return lines.OrderBy(w => w.BoundingBox.BottomLeft.X).ToList();

                case TextOrientation.Other:
                default:
                    // We consider the lines roughly have the same rotation.
                    var avgAngle = lines.Average(w => w.BoundingBox.Rotation);
                    if (double.IsNaN(avgAngle))
                    {
                        throw new NotFiniteNumberException("DocstrumBoundingBoxes: NaN bounding box rotation found when ordering lines.", avgAngle);
                    }

                    if (0 < avgAngle && avgAngle <= 90)
                    {
                        // quadrant 1, 0 < θ < π/2
                        return lines.OrderByDescending(w => w.BoundingBox.BottomLeft.Y).ThenBy(w => w.BoundingBox.BottomLeft.X).ToList();
                    }
                    else if (90 < avgAngle && avgAngle <= 180)
                    {
                        // quadrant 2, π/2 < θ ≤ π
                        return lines.OrderBy(w => w.BoundingBox.BottomLeft.X).ThenBy(w => w.BoundingBox.BottomLeft.Y).ToList();
                    }
                    else if (-180 < avgAngle && avgAngle <= -90)
                    {
                        // quadrant 3, -π < θ < -π/2
                        return lines.OrderBy(w => w.BoundingBox.BottomLeft.Y).ThenByDescending(w => w.BoundingBox.BottomLeft.X).ToList();
                    }
                    else if (-90 < avgAngle && avgAngle <= 0)
                    {
                        // quadrant 4, -π/2 < θ < 0
                        return lines.OrderByDescending(w => w.BoundingBox.BottomLeft.X).ThenByDescending(w => w.BoundingBox.BottomLeft.Y).ToList();
                    }
                    else
                    {
                        throw new ArgumentException("DocstrumBoundingBoxes: unknown bounding box rotation found when ordering lines.", nameof(avgAngle));
                    }
            }
        }
    }
}
