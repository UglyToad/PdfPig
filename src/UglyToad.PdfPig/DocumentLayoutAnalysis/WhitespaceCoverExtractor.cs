namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Content;
    using Geometry;

    /// <summary>
    /// A top-down algorithm that finds a cover of the background whitespace of a document in terms of maximal empty rectangles.
    /// <para>See Section 3.2 of 'High precision text extraction from PDF documents' by Øyvind Raddum Berg and Section 2 of 'Two geometric algorithms for layout analysis' by Thomas M. Breuel.</para>
    /// </summary>
    public static class WhitespaceCoverExtractor
    {
        /// <summary>
        /// Gets the cover of the background whitespace of a page in terms of maximal empty rectangles.
        /// </summary>
        /// <param name="words">The words in the page.</param>
        /// <param name="images">The images in the page.</param>
        /// <param name="maxRectangleCount">The maximum number of rectangles to find.</param>
        /// <param name="maxBoundQueueSize">The maximum size of the queue used in the algorithm.</param>
        /// <returns>The identified whitespace rectangles.</returns>
        public static IReadOnlyList<PdfRectangle> GetWhitespaces(IEnumerable<Word> words, IEnumerable<IPdfImage> images = null, int maxRectangleCount = 40, int maxBoundQueueSize = 0)
        {
            return GetWhitespaces(words,
                                  images,
                                  words.SelectMany(w => w.Letters).Select(x => x.GlyphRectangle.Width).Mode() * 1.25,
                                  words.SelectMany(w => w.Letters).Select(x => x.GlyphRectangle.Height).Mode() * 1.25,
                                  maxRectangleCount: maxRectangleCount,
                                  maxBoundQueueSize: maxBoundQueueSize);
        }

        /// <summary>
        /// Gets the cover of the background whitespace of a page in terms of maximal empty rectangles.
        /// </summary>
        /// <param name="words">The words in the page.</param>
        /// <param name="images">The images in the page.</param>
        /// <param name="minWidth">Lower bounds for the width of rectangles.</param>
        /// <param name="minHeight">Lower bounds for the height of rectangles.</param>
        /// <param name="maxRectangleCount">The maximum number of rectangles to find.</param>
        /// <param name="whitespaceFuzziness">Constant value to allow candidate whitespace rectangle to overlap the 
        /// surrounding obstacles by some percent. Default value is 15%.</param>
        /// <param name="maxBoundQueueSize">The maximum size of the queue used in the algorithm.</param>
        /// <returns>The identified whitespace rectangles.</returns>
        public static IReadOnlyList<PdfRectangle> GetWhitespaces(IEnumerable<Word> words, IEnumerable<IPdfImage> images,
            double minWidth, double minHeight, int maxRectangleCount = 40, double whitespaceFuzziness = 0.15, int maxBoundQueueSize = 0)
        {
            var bboxes = words.Where(w => w.BoundingBox.Width > 0 && w.BoundingBox.Height > 0)
                .Select(o => o.BoundingBox).ToList();

            if (images != null && images.Count() > 0)
            {
                bboxes.AddRange(images.Where(w => w.Bounds.Width > 0 && w.Bounds.Height > 0).Select(o => o.Bounds));
            }

            return GetWhitespaces(bboxes,
                                  minWidth: minWidth,
                                  minHeight: minHeight,
                                  maxRectangleCount: maxRectangleCount,
                                  whitespaceFuzziness: whitespaceFuzziness,
                                  maxBoundQueueSize: maxBoundQueueSize);
        }

        /// <summary>
        /// Gets the cover of the background whitespace of a page in terms of maximal empty rectangles.
        /// </summary>
        /// <param name="boundingboxes">The list of obstacles' bounding boxes in the page.</param>
        /// <param name="minWidth">Lower bounds for the width of rectangles.</param>
        /// <param name="minHeight">Lower bounds for the height of rectangles.</param>
        /// <param name="maxRectangleCount">The maximum number of rectangles to find.</param>
        /// <param name="whitespaceFuzziness">Constant value to allow candidate whitespace rectangle to overlap the 
        /// surrounding obstacles by some percent. Default value is 15%.</param>
        /// <param name="maxBoundQueueSize">The maximum size of the queue used in the algorithm.</param>
        /// <returns>The identified whitespace rectangles.</returns>
        public static IReadOnlyList<PdfRectangle> GetWhitespaces(IEnumerable<PdfRectangle> boundingboxes,
            double minWidth, double minHeight, int maxRectangleCount = 40, double whitespaceFuzziness = 0.15, int maxBoundQueueSize = 0)
        {
            if (boundingboxes.Count() == 0) return EmptyArray<PdfRectangle>.Instance;

            var obstacles = new HashSet<PdfRectangle>(boundingboxes);
            var pageBound = GetBound(obstacles);
            return GetMaximalRectangles(pageBound,
                                        obstacles,
                                        minWidth: minWidth,
                                        minHeight: minHeight,
                                        maxRectangleCount: maxRectangleCount,
                                        whitespaceFuzziness: whitespaceFuzziness,
                                        maxBoundQueueSize: maxBoundQueueSize);
        }

        private static IReadOnlyList<PdfRectangle> GetMaximalRectangles(PdfRectangle bound,
            HashSet<PdfRectangle> obstacles, double minWidth, double minHeight, int maxRectangleCount,
            double whitespaceFuzziness, int maxBoundQueueSize)
        {
            QueueEntries queueEntries = new QueueEntries(maxBoundQueueSize);
            queueEntries.Enqueue(new QueueEntry(bound, obstacles, whitespaceFuzziness));

            HashSet<PdfRectangle> selected = new HashSet<PdfRectangle>();
            HashSet<QueueEntry> holdList = new HashSet<QueueEntry>();

            while (queueEntries.Any())
            {
                var current = queueEntries.Dequeue();

                if (current.IsEmptyEnough(obstacles))
                {
                    if (selected.Any(c => Inside(c, current.Bound))) continue;

                    // A check was added which impeded the algorithm from accepting
                    // rectangles which were not adjacent to an already accepted 
                    // rectangle, or to the border of the page.
                    if (!IsAdjacentToPageBounds(bound, current.Bound) &&        // NOT in contact to border page AND
                        !selected.Any(q => IsAdjacentTo(q, current.Bound)))     // NOT in contact to any already accepted rectangle
                    {
                        // In order to maintain the correctness of the algorithm, 
                        // rejected rectangles are put in a hold list. 
                        holdList.Add(current);
                        continue;
                    }

                    selected.Add(current.Bound);

                    if (selected.Count >= maxRectangleCount) return selected.ToList();

                    obstacles.Add(current.Bound);

                    // Each time a new rectangle is identified and accepted, this hold list 
                    // will be added back to the queue in case any of them will have become valid.
                    foreach (var hold in holdList)
                    {
                        queueEntries.Enqueue(hold);
                    }

                    // After a maximal rectangle has been found, it is added back to the list 
                    // of obstacles. Whenever a QueueEntry is dequeued, its list of obstacles 
                    // can be recomputed to include newly identified whitespace rectangles.
                    foreach (var overlapping in queueEntries)
                    {
                        if (OverlapsHard(current.Bound, overlapping.Bound))
                            overlapping.AddWhitespace(current.Bound);
                    }

                    continue;
                }

                var pivot = current.GetPivot();
                var b = current.Bound;

                List<PdfRectangle> subRectangles = new List<PdfRectangle>();

                var rRight = new PdfRectangle(pivot.Right, b.Bottom, b.Right, b.Top);
                if (b.Right > pivot.Right && rRight.Height > minHeight && rRight.Width > minWidth)
                {
                    queueEntries.Enqueue(new QueueEntry(rRight,
                        new HashSet<PdfRectangle>(current.Obstacles.Where(o => OverlapsHard(rRight, o))),
                        whitespaceFuzziness));
                }

                var rLeft = new PdfRectangle(b.Left, b.Bottom, pivot.Left, b.Top);
                if (b.Left < pivot.Left && rLeft.Height > minHeight && rLeft.Width > minWidth)
                {
                    queueEntries.Enqueue(new QueueEntry(rLeft,
                        new HashSet<PdfRectangle>(current.Obstacles.Where(o => OverlapsHard(rLeft, o))),
                        whitespaceFuzziness));
                }

                var rAbove = new PdfRectangle(b.Left, b.Bottom, b.Right, pivot.Bottom);
                if (b.Bottom < pivot.Bottom && rAbove.Height > minHeight && rAbove.Width > minWidth)
                {
                    queueEntries.Enqueue(new QueueEntry(rAbove,
                        new HashSet<PdfRectangle>(current.Obstacles.Where(o => OverlapsHard(rAbove, o))),
                        whitespaceFuzziness));
                }

                var rBelow = new PdfRectangle(b.Left, pivot.Top, b.Right, b.Top);
                if (b.Top > pivot.Top && rBelow.Height > minHeight && rBelow.Width > minWidth)
                {
                    queueEntries.Enqueue(new QueueEntry(rBelow,
                        new HashSet<PdfRectangle>(current.Obstacles.Where(o => OverlapsHard(rBelow, o))),
                        whitespaceFuzziness));
                }
            }

            return selected.ToList();
        }

        private static bool IsAdjacentTo(PdfRectangle rectangle1, PdfRectangle rectangle2)
        {
            if (rectangle1.Left > rectangle2.Right ||
                rectangle2.Left > rectangle1.Right ||
                rectangle1.Top < rectangle2.Bottom ||
                rectangle2.Top < rectangle1.Bottom)
            {
                return false;
            }

            if (rectangle1.Left == rectangle2.Right ||
                rectangle1.Right == rectangle2.Left ||
                rectangle1.Bottom == rectangle2.Top ||
                rectangle1.Top == rectangle2.Bottom)
            {
                return true;
            }
            return false;
        }

        private static bool IsAdjacentToPageBounds(PdfRectangle pageBound, PdfRectangle rectangle)
        {
            if (rectangle.Bottom == pageBound.Bottom ||
                rectangle.Top == pageBound.Top ||
                rectangle.Left == pageBound.Left ||
                rectangle.Right == pageBound.Right)
            {
                return true;
            }

            return false;
        }

        private static bool OverlapsHard(PdfRectangle rectangle1, PdfRectangle rectangle2)
        {
            if (rectangle1.Left >= rectangle2.Right ||
                rectangle2.Left >= rectangle1.Right ||
                rectangle1.Top <= rectangle2.Bottom ||
                rectangle2.Top <= rectangle1.Bottom)
            {
                return false;
            }

            return true;
        }

        private static bool Inside(PdfRectangle rectangle1, PdfRectangle rectangle2)
        {
            if (rectangle2.Right <= rectangle1.Right && rectangle2.Left >= rectangle1.Left &&
                rectangle2.Top <= rectangle1.Top && rectangle2.Bottom >= rectangle1.Bottom)
            {
                return true;
            }

            return false;
        }

        private static PdfRectangle GetBound(IEnumerable<PdfRectangle> obstacles)
        {
            return new PdfRectangle(
                obstacles.Min(b => b.Left),
                obstacles.Min(b => b.Bottom),
                obstacles.Max(b => b.Right),
                obstacles.Max(b => b.Top));
        }

        #region Sorted Queue
        private class QueueEntries : SortedSet<QueueEntry>
        {
            int bound;

            public QueueEntries(int maximumBound)
            {
                bound = maximumBound;
            }

            public QueueEntry Dequeue()
            {
                var current = Max;
                Remove(current);
                return current;
            }

            public void Enqueue(QueueEntry queueEntry)
            {
                if (bound > 0 && Count > bound)
                {
                    Remove(Min);
                }
                Add(queueEntry);
            }
        }

        private class QueueEntry : IComparable<QueueEntry>
        {
            private readonly double quality;
            private readonly double whitespaceFuzziness;

            public PdfRectangle Bound { get; }

            public HashSet<PdfRectangle> Obstacles { get; }

            public QueueEntry(PdfRectangle bound, HashSet<PdfRectangle> obstacles, double whitespaceFuzziness)
            {
                Bound = bound;
                quality = ScoringFunction(Bound);
                Obstacles = obstacles;
                this.whitespaceFuzziness = whitespaceFuzziness;
            }

            public PdfRectangle GetPivot()
            {
                int indexMiddle = Distances.FindIndexNearest(Bound.Centroid,
                                    Obstacles.Select(o => o.Centroid).ToList(),
                                    p => p, p => p, Distances.Euclidean, out double d);

                return indexMiddle == -1 ? Obstacles.First() : Obstacles.ElementAt(indexMiddle);
            }

            public bool IsEmptyEnough()
            {
                return !Obstacles.Any();
            }

            public bool IsEmptyEnough(IEnumerable<PdfRectangle> pageObstacles)
            {
                if (IsEmptyEnough()) return true;

                double sum = 0;
                foreach (var obstacle in pageObstacles)
                {
                    var intersect = Bound.Intersect(obstacle);
                    if (!intersect.HasValue) return false;

                    double minimumArea = MinimumOverlappingArea(obstacle, Bound, whitespaceFuzziness);

                    if (intersect.Value.Area > minimumArea)
                    {
                        return false;
                    }
                    sum += intersect.Value.Area;
                }
                return sum < Bound.Area * whitespaceFuzziness;
            }

            public override string ToString()
            {
                return "Q=" + quality.ToString("#0.0") + ", O=" + Obstacles.Count + ", " + Bound.ToString();
            }

            public void AddWhitespace(PdfRectangle rectangle)
            {
                Obstacles.Add(rectangle);
            }

            public int CompareTo(QueueEntry entry)
            {
                return quality.CompareTo(entry.quality);
            }

            public override bool Equals(object obj)
            {
                if (obj is QueueEntry entry)
                {
                    if (Bound.Left != entry.Bound.Left ||
                        Bound.Right != entry.Bound.Right ||
                        Bound.Top != entry.Bound.Top ||
                        Bound.Bottom != entry.Bound.Bottom ||
                        Obstacles != entry.Obstacles) return false;
                    return true;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return (Bound.Left, Bound.Right,
                        Bound.Top, Bound.Bottom,
                        Obstacles).GetHashCode();
            }

            private static double MinimumOverlappingArea(PdfRectangle r1, PdfRectangle r2, double whitespaceFuzziness)
            {
                return Math.Min(r1.Area, r2.Area) * whitespaceFuzziness;
            }

            /// <summary>
            /// The scoring function Q(r) which is subsequently used to sort a priority queue.
            /// </summary>
            /// <param name="rectangle"></param>
            private static double ScoringFunction(PdfRectangle rectangle)
            {
                // As can be seen, tall rectangles are preferred. The trick while choosing this Q(r) was
                // to keep that preference while still allowing wide rectangles to be chosen. After having
                // experimented with quite a few variations, this simple function was considered a good
                // solution.
                return rectangle.Area * (rectangle.Height / 4.0);
            }

            private static double OverlappingArea(PdfRectangle rectangle1, PdfRectangle rectangle2)
            {
                var intersect = rectangle1.Intersect(rectangle2);
                if (intersect.HasValue)
                {
                    return intersect.Value.Area;
                }
                return 0;
            }
        }
        #endregion
    }
}
