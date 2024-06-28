namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using UglyToad.PdfPig.Content;

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

        private Func<IBoundingBox, IBoundingBox, double, bool> getBeforeInMethod;

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

            getBeforeInMethod = GetBeforeInMethod();
        }

        private Func<IBoundingBox, IBoundingBox, double, bool> GetBeforeInMethod()
        {
            switch (SpatialReasoningRule)
            {
                case SpatialReasoningRules.ColumnWise:
                    if (UseRenderingOrder)
                    {
                        // Important note: GetBeforeInRendering will return false if type is not TextBox meaning it's result gets ignored 
                        return (IBoundingBox a, IBoundingBox b, double t) => GetBeforeInReadingVertical(a, b, t)
                                                    || GetBeforeInRendering(a, b);
                    }
                    else
                    {
                        return GetBeforeInReadingVertical;
                    }
                case SpatialReasoningRules.RowWise:
                    if (UseRenderingOrder)
                    {
                        return (IBoundingBox a, IBoundingBox b, double t) => GetBeforeInReadingHorizontal(a, b, t)
                                                    || GetBeforeInRendering(a, b);
                    }
                    else
                    {
                        return GetBeforeInReadingHorizontal;
                    }
                case SpatialReasoningRules.Basic:
                default:
                    if (UseRenderingOrder)
                    {
                        return (IBoundingBox a, IBoundingBox b, double t) => GetBeforeInReading(a, b, t)
                                                    || GetBeforeInRendering(a, b);
                    }
                    else
                    {
                        return GetBeforeInReading;
                    }
            }
        }

        /// <summary>
        /// Gets the blocks ordered in reading order. 
        /// If blocks are of type <see cref="TextBlock"/> it will also set the <see cref="TextBlock.ReadingOrder"/>.
        /// </summary>
        /// <param name="inBlocks">The blocks to order.</param>
        public IEnumerable<TBlock> Get<TBlock>(IEnumerable<TBlock> inBlocks)
            where TBlock : IBoundingBox
        {
            IReadOnlyList<TBlock> blocks = new ReadOnlyCollection<TBlock>(inBlocks.ToList());
            int readingOrder = 0;

            var graph = BuildGraph(blocks, T);

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

                var block = blocks[index];
                if(block is TextBlock textBlock)
                {
                    textBlock.SetReadingOrder(readingOrder++);
                }

                yield return block;
            }
        }

        private Dictionary<int, List<int>> BuildGraph<TBlock>(IReadOnlyList<TBlock> blocks, double T)
             where TBlock : IBoundingBox
        {
            // We incorporate both relations into a single partial ordering of blocks by specifying a 
            // directed graph with an edge between every pair of blocks for which at least one of the 
            // two relations hold.

            var graph = new Dictionary<int, List<int>>();

            for (int i = 0; i < blocks.Count; i++)
            {
                graph.Add(i, new List<int>());
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                var a = blocks[i];
                for (int j = 0; j < blocks.Count; j++)
                {
                    if (i == j) continue;
                    var b = blocks[j];

                    if (getBeforeInMethod(a, b, T))
                    {
                        graph[i].Add(j);
                    }
                }
            }

            return graph;
        }


        /// <summary>
        /// Gets before in Rendering order. This only works on classes that implement <see cref="ILettersBlock"/>
        /// </summary>
        /// <returns>Text Before in rendering. False if type does not implement <see cref="ILettersBlock"/></returns>
        private static bool GetBeforeInRendering(IBoundingBox alpha, IBoundingBox bravo)
        {
            if (alpha is ILettersBlock a && bravo is ILettersBlock b)
            {
                var avgTextSequenceA = a.Letters.Average(x => x.TextSequence);
                var avgTextSequenceB = b.Letters.Average(x => x.TextSequence);
                return avgTextSequenceA < avgTextSequenceB;
            }
 
            return false;
        }

        /// <summary>
        /// Rule encoding the fact that in western culture the reading order is from left to right and from top to bottom.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="T">The tolerance parameter T.</param>
        private static bool GetBeforeInReading(IBoundingBox a, IBoundingBox b, double T)
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
        private static bool GetBeforeInReadingVertical(IBoundingBox a, IBoundingBox b, double T)
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
        private static bool GetBeforeInReadingHorizontal(IBoundingBox a, IBoundingBox b, double T)
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