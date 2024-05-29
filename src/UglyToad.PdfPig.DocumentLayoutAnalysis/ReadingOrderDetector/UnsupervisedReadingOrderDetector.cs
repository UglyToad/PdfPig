namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Algorithm that retrieve the blocks' reading order using spatial reasoning (Allen’s interval relations) and possibly the rendering order (TextSequence).
    /// <para>See section 4.1 of 'Unsupervised document structure analysis of digital scientific articles' by S. Klampfl, M. Granitzer, K. Jack, R. Kern
    /// and 'Document Understanding for a Broad Class of Documents' by L. Todoran, M. Worring, M. Aiello and C. Monz.</para>
    /// </summary>
    public class UnsupervisedReadingOrderDetector : IReadingOrderDetector
    {
        /// <summary>
        /// The rules encoding the spatial reasoning constraints.
        /// <para>See 'Document Understanding for a Broad Class of Documents' by L. Todoran, M. Worring, M. Aiello and C. Monz.</para>
        /// </summary>
        public enum SpatialReasoningRules
        {
            /// <summary>
            /// Basic spacial reasoning.
            /// <para>In western culture the reading order is from left to right and from top to bottom.</para>
            /// </summary>
            Basic = 0,

            /// <summary>
            /// Text-blocks are read in rows from left-to-right, top-to-bottom.
            /// <para>The diagonal direction 'left-bottom to top-right' cannot be present among the Basic relations allowed.</para>
            /// </summary>
            RowWise = 1,

            /// <summary>
            /// Text-blocks are read in columns, from top-to-bottom and from left-to-right.
            /// <para>The diagonal direction 'right-top to bottom-left' cannot be present among the Basic relations allowed.</para>
            /// </summary>
            ColumnWise = 2
        }

        /// <summary>
        /// Create an instance of unsupervised reading order detector, <see cref="UnsupervisedReadingOrderDetector"/>.
        /// <para>This detector uses spatial reasoning (Allen’s interval relations) and possibly the rendering order (TextSequence).</para>
        /// </summary>
        public static UnsupervisedReadingOrderDetector Instance { get; } = new UnsupervisedReadingOrderDetector();

        /// <summary>
        /// Whether or not to also use the rendering order, as indicated by the TextSequence.
        /// </summary>
        public bool UseRenderingOrder { get; }

        /// <summary>
        /// The rule to be used that encodes the spatial reasoning constraints.
        /// </summary>
        public SpatialReasoningRules SpatialReasoningRule { get; }

        /// <summary>
        /// The tolerance parameter T. If two coordinates are closer than T they are considered equal.
        /// <para>This flexibility is necessary because due to the inherent noise in the PDF extraction text blocks in the
        /// same column might not be exactly aligned.</para>
        /// </summary>
        public double T { get; }

        private Func<TextBlock, TextBlock, double, bool> getBeforeInMethod;

        /// <summary>
        /// Algorithm that retrieve the blocks' reading order using spatial reasoning (Allen’s interval relations) and possibly the rendering order (TextSequence).
        /// </summary>
        /// <param name="T">The tolerance parameter T. If two coordinates are closer than T they are considered equal.
        /// This flexibility is necessary because due to the inherent noise in the PDF extraction text blocks in the
        /// same column might not be exactly aligned.</param>
        /// <param name="spatialReasoningRule">The rule to be used that encodes the spatial reasoning constraints.</param>
        /// <param name="useRenderingOrder">Whether or not to also use the rendering order, as indicated by the TextSequence.</param>
        public UnsupervisedReadingOrderDetector(double T = 5, SpatialReasoningRules spatialReasoningRule = SpatialReasoningRules.ColumnWise, bool useRenderingOrder = true)
        {
            this.T = T;
            this.SpatialReasoningRule = spatialReasoningRule;
            this.UseRenderingOrder = useRenderingOrder;

            switch (SpatialReasoningRule)
            {
                case SpatialReasoningRules.ColumnWise:
                    if (UseRenderingOrder)
                    {
                        getBeforeInMethod = (TextBlock a, TextBlock b, double t) => GetBeforeInReadingVertical(a, b, t) || GetBeforeInRendering(a, b);
                    }
                    else
                    {
                        getBeforeInMethod = GetBeforeInReadingVertical;
                    }
                    break;

                case SpatialReasoningRules.RowWise:
                    if (UseRenderingOrder)
                    {
                        getBeforeInMethod = (TextBlock a, TextBlock b, double t) => GetBeforeInReadingHorizontal(a, b, t) || GetBeforeInRendering(a, b);
                    }
                    else
                    {
                        getBeforeInMethod = GetBeforeInReadingHorizontal;
                    }
                    break;

                case SpatialReasoningRules.Basic:
                default:
                    if (UseRenderingOrder)
                    {
                        getBeforeInMethod = (TextBlock a, TextBlock b, double t) => GetBeforeInReading(a, b, t) || GetBeforeInRendering(a, b);
                    }
                    else
                    {
                        getBeforeInMethod = GetBeforeInReading;
                    }
                    break;
            }
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

                    if (getBeforeInMethod(a, b, T))
                    {
                        graph[i].Add(j);
                    }
                }
            }

            return graph;
        }

        private static bool GetBeforeInRendering(TextBlock a, TextBlock b)
        {
            var avgTextSequenceA = a.TextLines.SelectMany(tl => tl.Words).SelectMany(w => w.Letters).Select(l => l.TextSequence).Average();
            var avgTextSequenceB = b.TextLines.SelectMany(tl => tl.Words).SelectMany(w => w.Letters).Select(l => l.TextSequence).Average();
            return avgTextSequenceA < avgTextSequenceB;
        }

        /// <summary>
        /// Rule encoding the fact that in western culture the reading order is from left to right and from top to bottom.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="T">The tolerance parameter T.</param>
        private static bool GetBeforeInReading(TextBlock a, TextBlock b, double T)
        {
            IntervalRelations xRelation = IntervalRelationsHelper.GetRelationX(a.BoundingBox, b.BoundingBox, T);
            IntervalRelations yRelation = IntervalRelationsHelper.GetRelationY(a.BoundingBox, b.BoundingBox, T);

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
        private static bool GetBeforeInReadingVertical(TextBlock a, TextBlock b, double T)
        {
            IntervalRelations xRelation = IntervalRelationsHelper.GetRelationX(a.BoundingBox, b.BoundingBox, T);
            IntervalRelations yRelation = IntervalRelationsHelper.GetRelationY(a.BoundingBox, b.BoundingBox, T);

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
        private static bool GetBeforeInReadingHorizontal(TextBlock a, TextBlock b, double T)
        {
            IntervalRelations xRelation = IntervalRelationsHelper.GetRelationX(a.BoundingBox, b.BoundingBox, T);
            IntervalRelations yRelation = IntervalRelationsHelper.GetRelationY(a.BoundingBox, b.BoundingBox, T);

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

    }
}
