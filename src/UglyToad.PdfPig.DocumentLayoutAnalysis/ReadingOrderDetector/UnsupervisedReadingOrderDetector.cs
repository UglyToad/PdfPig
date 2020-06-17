namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Algorithm that retrieve the blocks' reading order using both (spatial) Allen’s interval relations and rendering order (TextSequence).
    /// <para>See section 5.1 of 'Unsupervised document structure analysis of digital scientific articles' by S. Klampfl, M. Granitzer, K. Jack, R. Kern and 'Document Understanding for a Broad Class of Documents' by L. Todoran, M. Worring, M. Aiello and C. Monz.</para>
    /// </summary>
    public class UnsupervisedReadingOrderDetector : IReadingOrderDetector
    {
        /// <summary>
        /// Create an instance of unsupervised reading order detector, <see cref="UnsupervisedReadingOrderDetector"/>.
        /// <para>This detector uses the (spatial) Allen’s interval relations and rendering order (TextSequence).</para>
        /// </summary>
        public static UnsupervisedReadingOrderDetector Instance { get; } = new UnsupervisedReadingOrderDetector();

        private readonly double T;

        /// <summary>
        /// Algorithm that retrieve the blocks' reading order using both (spatial) Allen’s interval relations and rendering order.
        /// </summary>
        /// <param name="T">The tolerance parameter T. If two coordinates are closer than T they are considered equal.
        /// This flexibility is necessary because due to the inherent noise in the PDF extraction text blocks in the
        /// same column might not be exactly aligned.</param>
        public UnsupervisedReadingOrderDetector(double T = 5)
        {
            this.T = T;
        }

        /// <summary>
        /// Gets the blocks in reading order and sets the <see cref="TextBlock.ReadingOrder"/>.
        /// </summary>
        /// <param name="textBlocks">The <see cref="TextBlock"/>s to order.</param>
        public IEnumerable<TextBlock> Get(IReadOnlyList<TextBlock> textBlocks)
        {
            int readingOrder = 0;

            var graph = BuildGraph(textBlocks, T);

            while (graph.Count > 0)
            {
                var maxCount = graph.Max(kvp => kvp.Value.Count);
                var current = graph.FirstOrDefault(kvp => kvp.Value.Count == maxCount);
                graph.Remove(current.Key);
                int index = current.Key;

                foreach (var g in graph)
                {
                    g.Value.Remove(index);
                }

                var block = textBlocks[index];
                block.SetReadingOrder(readingOrder++);

                yield return block;
            }
        }

        private Dictionary<int, List<int>> BuildGraph(IReadOnlyList<TextBlock> textBlocks, double T)
        {
            // We incorporate both relations into a single partial ordering of blocks by specifying a 
            // directed graph with an edge between every pair of blocks for which at least one of the 
            // two relations hold.

            var graph = new Dictionary<int, List<int>>();

            for (int i = 0; i < textBlocks.Count; i++)
            {
                graph.Add(i, new List<int>());
            }

            for (int i = 0; i < textBlocks.Count; i++)
            {
                var a = textBlocks[i];
                for (int j = 0; j < textBlocks.Count; j++)
                {
                    if (i == j) continue;
                    var b = textBlocks[j];

                    if (GetBeforeInReadingRendering(a, b, T))
                    {
                        graph[i].Add(j);
                    }
                }
            }

            return graph;
        }

        private bool GetBeforeInReadingRendering(TextBlock a, TextBlock b, double T)
        {
            return GetBeforeInReadingVertical(a, b, T) || GetBeforeInRendering(a, b);
        }

        private bool GetBeforeInRendering(TextBlock a, TextBlock b)
        {
            var avgTextSequenceA = a.TextLines.SelectMany(tl => tl.Words).SelectMany(w => w.Letters).Select(l => l.TextSequence).Average();
            var avgTextSequenceB = b.TextLines.SelectMany(tl => tl.Words).SelectMany(w => w.Letters).Select(l => l.TextSequence).Average();
            return avgTextSequenceA < avgTextSequenceB;
        }

        private bool GetBeforeInReading(TextBlock a, TextBlock b, double T)
        {
            IntervalRelations xRelation = GetIntervalRelationX(a, b, T);
            IntervalRelations yRelation = GetIntervalRelationY(a, b, T);

            return xRelation == IntervalRelations.Precedes ||
                   yRelation == IntervalRelations.Precedes ||
                   xRelation == IntervalRelations.Meets ||
                   yRelation == IntervalRelations.Meets ||
                   xRelation == IntervalRelations.Overlaps ||
                   yRelation == IntervalRelations.Overlaps;
        }

        /// <summary>
        /// Column-wise: text-blocks are read in columns, from top-to-bottom and from left-to-right.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="T">The tolerance parameter T.</param>
        /// <returns></returns>
        private bool GetBeforeInReadingVertical(TextBlock a, TextBlock b, double T)
        {
            IntervalRelations xRelation = GetIntervalRelationX(a, b, T);
            IntervalRelations yRelation = GetIntervalRelationY(a, b, T);

            return xRelation == IntervalRelations.Precedes ||
                xRelation == IntervalRelations.Meets ||
                (xRelation == IntervalRelations.Overlaps && (yRelation == IntervalRelations.Precedes ||
                                                             yRelation == IntervalRelations.Meets ||
                                                             yRelation == IntervalRelations.Overlaps)) ||
                ((yRelation == IntervalRelations.Precedes || yRelation == IntervalRelations.Meets || yRelation == IntervalRelations.Overlaps) &&
                                                            (xRelation == IntervalRelations.Precedes ||
                                                             xRelation == IntervalRelations.Meets ||
                                                             xRelation == IntervalRelations.Overlaps ||
                                                             xRelation == IntervalRelations.Starts ||
                                                             xRelation == IntervalRelations.FinishesI ||
                                                             xRelation == IntervalRelations.Equals ||
                                                             xRelation == IntervalRelations.During ||
                                                             xRelation == IntervalRelations.DuringI ||
                                                             xRelation == IntervalRelations.Finishes ||
                                                             xRelation == IntervalRelations.StartsI ||
                                                             xRelation == IntervalRelations.OverlapsI));
        }

        /// <summary>
        /// Row-wise: text-blocks are read in rows from left-to-right, top- to-bottom.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="T">The tolerance parameter T.</param>
        private bool GetBeforeInReadingHorizontal(TextBlock a, TextBlock b, double T)
        {
            IntervalRelations xRelation = GetIntervalRelationX(a, b, T);
            IntervalRelations yRelation = GetIntervalRelationY(a, b, T);

            return yRelation == IntervalRelations.Precedes ||
                   yRelation == IntervalRelations.Meets ||
                    (yRelation == IntervalRelations.Overlaps && (xRelation == IntervalRelations.Precedes ||
                                                                 xRelation == IntervalRelations.Meets ||
                                                                 xRelation == IntervalRelations.Overlaps)) ||
                    ((xRelation == IntervalRelations.Precedes || xRelation == IntervalRelations.Meets || xRelation == IntervalRelations.Overlaps) &&
                                                                (yRelation == IntervalRelations.Precedes ||
                                                                 yRelation == IntervalRelations.Meets ||
                                                                 yRelation == IntervalRelations.Overlaps ||
                                                                 yRelation == IntervalRelations.Starts ||
                                                                 yRelation == IntervalRelations.FinishesI ||
                                                                 yRelation == IntervalRelations.Equals ||
                                                                 yRelation == IntervalRelations.During ||
                                                                 yRelation == IntervalRelations.DuringI ||
                                                                 yRelation == IntervalRelations.Finishes ||
                                                                 yRelation == IntervalRelations.StartsI ||
                                                                 yRelation == IntervalRelations.OverlapsI));
        }

        /// <summary>
        /// Gets the Thick Boundary Rectangle Relations (TBRR) for the X coordinate.
        /// <para>The Thick Boundary Rectangle Relations (TBRR) is a set of qualitative relations representing the spatial relations of the document objects on the page.
        /// For every pair of document objects a and b, one X and one Y interval relation hold. If one considers the pair in reversed
        /// order, the inverse interval relation holds. Therefore the directed graph g_i representing these relations is complete.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="T">The tolerance parameter T. If two coordinates are closer than T they are considered equal.</param>
        private IntervalRelations GetIntervalRelationX(TextBlock a, TextBlock b, double T)
        {
            if (a.BoundingBox.Right < b.BoundingBox.Left - T)
            {
                return IntervalRelations.Precedes;
            }
            else if (a.BoundingBox.Right >= b.BoundingBox.Left - T)
            {
                return IntervalRelations.PrecedesI;
            }

            else if (b.BoundingBox.Left - T <= a.BoundingBox.Right
                && a.BoundingBox.Right <= b.BoundingBox.Left + T)
            {
                return IntervalRelations.Meets;
            }
            else if (b.BoundingBox.Left - T > a.BoundingBox.Right
                && a.BoundingBox.Right > b.BoundingBox.Left + T)
            {
                return IntervalRelations.MeetsI;
            }

            else if (a.BoundingBox.Left < b.BoundingBox.Left - T
                && (b.BoundingBox.Left + T < a.BoundingBox.Right && a.BoundingBox.Right < b.BoundingBox.Right - T))
            {
                return IntervalRelations.Overlaps;
            }
            else if (a.BoundingBox.Left >= b.BoundingBox.Left - T
               && (b.BoundingBox.Left + T >= a.BoundingBox.Right && a.BoundingBox.Right >= b.BoundingBox.Right - T))
            {
                return IntervalRelations.OverlapsI;
            }

            else if (b.BoundingBox.Left - T <= a.BoundingBox.Left && a.BoundingBox.Left <= b.BoundingBox.Left + T
                && a.BoundingBox.Right < b.BoundingBox.Right - T)
            {
                return IntervalRelations.Starts;
            }
            else if (b.BoundingBox.Left - T > a.BoundingBox.Left && a.BoundingBox.Left > b.BoundingBox.Left + T
                && a.BoundingBox.Right >= b.BoundingBox.Right - T)
            {
                return IntervalRelations.StartsI;
            }

            else if (a.BoundingBox.Left > b.BoundingBox.Left + T
                && a.BoundingBox.Right < b.BoundingBox.Right - T)
            {
                return IntervalRelations.During;
            }
            else if (a.BoundingBox.Left <= b.BoundingBox.Left + T
                && a.BoundingBox.Right >= b.BoundingBox.Right - T)
            {
                return IntervalRelations.DuringI;
            }

            else if (a.BoundingBox.Left > b.BoundingBox.Left + T
                && (b.BoundingBox.Right - T <= a.BoundingBox.Right && a.BoundingBox.Right <= b.BoundingBox.Right + T))
            {
                return IntervalRelations.Finishes;
            }
            else if (a.BoundingBox.Left <= b.BoundingBox.Left + T
                && (b.BoundingBox.Right - T > a.BoundingBox.Right && a.BoundingBox.Right > b.BoundingBox.Right + T))
            {
                return IntervalRelations.FinishesI;
            }

            else if (b.BoundingBox.Left - T <= a.BoundingBox.Left && a.BoundingBox.Left <= b.BoundingBox.Left + T
                && (b.BoundingBox.Right - T <= a.BoundingBox.Right && a.BoundingBox.Right <= b.BoundingBox.Right + T))
            {
                return IntervalRelations.Equals;
            }

            return IntervalRelations.Unknown;
        }

        /// <summary>
        /// Gets the Thick Boundary Rectangle Relations (TBRR) for the Y coordinate.
        /// <para>The Thick Boundary Rectangle Relations (TBRR) is a set of qualitative relations representing the spatial relations of the document objects on the page.
        /// For every pair of document objects a and b, one X and one Y interval relation hold. If one considers the pair in reversed
        /// order, the inverse interval relation holds. Therefore the directed graph g_i representing these relations is complete.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="T">The tolerance parameter T. If two coordinates are closer than T they are considered equal.</param>
        private IntervalRelations GetIntervalRelationY(TextBlock a, TextBlock b, double T)
        {
            if (a.BoundingBox.Bottom < b.BoundingBox.Top - T)
            {
                return IntervalRelations.PrecedesI;
            }
            else if (a.BoundingBox.Bottom >= b.BoundingBox.Top - T)
            {
                return IntervalRelations.Precedes;
            }

            else if (b.BoundingBox.Top - T <= a.BoundingBox.Bottom
                && a.BoundingBox.Bottom <= b.BoundingBox.Top + T)
            {
                return IntervalRelations.MeetsI;
            }
            else if (b.BoundingBox.Top - T > a.BoundingBox.Bottom
                && a.BoundingBox.Bottom > b.BoundingBox.Top + T)
            {
                return IntervalRelations.Meets;
            }

            else if (a.BoundingBox.Top < b.BoundingBox.Top - T
                && (b.BoundingBox.Top + T < a.BoundingBox.Bottom && a.BoundingBox.Bottom < b.BoundingBox.Bottom - T))
            {
                return IntervalRelations.OverlapsI;
            }
            else if (a.BoundingBox.Top >= b.BoundingBox.Top - T
               && (b.BoundingBox.Top + T >= a.BoundingBox.Bottom && a.BoundingBox.Bottom >= b.BoundingBox.Bottom - T))
            {
                return IntervalRelations.Overlaps;
            }

            else if (b.BoundingBox.Top - T <= a.BoundingBox.Top && a.BoundingBox.Top <= b.BoundingBox.Top + T
                && a.BoundingBox.Bottom < b.BoundingBox.Bottom - T)
            {
                return IntervalRelations.StartsI;
            }
            else if (b.BoundingBox.Top - T > a.BoundingBox.Top && a.BoundingBox.Top > b.BoundingBox.Top + T
                && a.BoundingBox.Bottom >= b.BoundingBox.Bottom - T)
            {
                return IntervalRelations.Starts;
            }

            else if (a.BoundingBox.Top > b.BoundingBox.Top + T
                && a.BoundingBox.Bottom < b.BoundingBox.Bottom - T)
            {
                return IntervalRelations.DuringI;
            }
            else if (a.BoundingBox.Top <= b.BoundingBox.Top + T
                && a.BoundingBox.Bottom >= b.BoundingBox.Bottom - T)
            {
                return IntervalRelations.During;
            }

            else if (a.BoundingBox.Top > b.BoundingBox.Top + T
                && (b.BoundingBox.Bottom - T <= a.BoundingBox.Bottom && a.BoundingBox.Bottom <= b.BoundingBox.Bottom + T))
            {
                return IntervalRelations.FinishesI;
            }
            else if (a.BoundingBox.Top <= b.BoundingBox.Top + T
                && (b.BoundingBox.Bottom - T > a.BoundingBox.Bottom && a.BoundingBox.Bottom > b.BoundingBox.Bottom + T))
            {
                return IntervalRelations.Finishes;
            }

            else if ((b.BoundingBox.Top - T <= a.BoundingBox.Top && a.BoundingBox.Top <= b.BoundingBox.Top + T)
                && (b.BoundingBox.Bottom - T <= a.BoundingBox.Bottom && a.BoundingBox.Bottom <= b.BoundingBox.Bottom + T))
            {
                return IntervalRelations.Equals;
            }

            return IntervalRelations.Unknown;
        }

        /// <summary>
        /// Allen’s interval thirteen relations.
        /// <para>See https://en.wikipedia.org/wiki/Allen%27s_interval_algebra</para>
        /// </summary>
        private enum IntervalRelations
        {
            /// <summary>
            /// Unknown interval relations.
            /// </summary>
            Unknown,

            /// <summary>
            /// X takes place before Y.
            /// <para>|____X____|......................</para>
            /// <para>......................|____Y____|</para>
            /// </summary>
            Precedes,

            /// <summary>
            /// X meets Y.
            /// <para>|____X____|.................</para>
            /// <para>.................|____Y____|</para>
            /// </summary>
            Meets,

            /// <summary>
            /// X overlaps with Y.
            /// <para>|______X______|.................</para>
            /// <para>.................|______Y______|</para>
            /// </summary>
            Overlaps,

            /// <summary>
            /// X starts Y.
            /// <para>|____X____|.................</para>
            /// <para>|_____Y_____|..............</para>
            /// </summary>
            Starts,

            /// <summary>
            /// X during Y.
            /// <para>........|____X____|.........</para>
            /// <para>.....|______Y______|.....</para>
            /// </summary>
            During,

            /// <summary>
            /// X finishes Y.
            /// <para>.................|____X____|</para>
            /// <para>..............|_____Y_____|</para>
            /// </summary>
            Finishes,

            /// <summary>
            /// Inverse precedes.
            /// </summary>
            PrecedesI,

            /// <summary>
            /// Inverse meets.
            /// </summary>
            MeetsI,

            /// <summary>
            /// Inverse overlaps.
            /// </summary>
            OverlapsI,

            /// <summary>
            /// Inverse Starts.
            /// </summary>
            StartsI,

            /// <summary>
            /// Inverse during.
            /// </summary>
            DuringI,

            /// <summary>
            /// Inverse finishes.
            /// </summary>
            FinishesI,

            /// <summary>
            /// X is equal to Y.
            /// <para>..........|____X____|............</para>
            /// <para>..........|____Y____|............</para>
            /// </summary>
            Equals
        }

        private class NodeComparer : IComparer<KeyValuePair<int, List<int>>>
        {
            public int Compare(KeyValuePair<int, List<int>> x, KeyValuePair<int, List<int>> y)
            {
                return x.Value.Count.CompareTo(y.Value.Count);
            }
        }
    }
}
