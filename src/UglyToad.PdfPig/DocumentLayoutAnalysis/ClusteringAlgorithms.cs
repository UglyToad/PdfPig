using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig.Geometry;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// Clustering Algorithms.
    /// </summary>
    internal class ClusteringAlgorithms
    {
        /// <summary>
        /// Algorithm to group elements via transitive closure, using nearest neighbours and maximum distance.
        /// https://en.wikipedia.org/wiki/Transitive_closure
        /// </summary>
        /// <typeparam name="T">Letter, Word, TextLine, etc.</typeparam>
        /// <param name="elements">List of elements to group.</param>
        /// <param name="distMeasure">The distance measure between two points.</param>
        /// <param name="maxDistanceFunction">The function that determines the maximum distance between two points in the same cluster.</param>
        /// <param name="pivotPoint">The pivot's point to use for pairing, e.g. BottomLeft, TopLeft.</param>
        /// <param name="candidatesPoint">The candidates' point to use for pairing, e.g. BottomLeft, TopLeft.</param>
        /// <param name="filterPivot">Filter to apply to the pivot point. If false, point will not be paired at all, e.g. is white space.</param>
        /// <param name="filterFinal">Filter to apply to both the pivot and the paired point. If false, point will not be paired at all, e.g. pivot and paired point have same font.</param>
        internal static IEnumerable<HashSet<int>> SimpleTransitiveClosure<T>(List<T> elements,
            Func<PdfPoint, PdfPoint, double> distMeasure,
            Func<T, T, double> maxDistanceFunction,
            Func<T, PdfPoint> pivotPoint, Func<T, PdfPoint> candidatesPoint,
            Func<T, bool> filterPivot, Func<T, T, bool> filterFinal)
        {
            /*************************************************************************************
             * Algorithm steps
             * 1. Find nearest neighbours indexes (done in parallel)
             *  Iterate every point (pivot) and put its nearest neighbour's index in an array
             *  e.g. if nearest neighbour of point i is point j, then indexes[i] = j.
             *  Only conciders a neighbour if it is within the maximum distance. 
             *  If not within the maximum distance, index will be set to -1.
             *  Each element has only one connected neighbour.
             *  NB: Given the possible asymmetry in the relationship, it is possible 
             *  that if indexes[i] = j then indexes[j] != i.
             *  
             * 2. Group indexes
             *  Group indexes if share neighbours in common - Transitive closure
             *  e.g. if we have indexes[i] = j, indexes[j] = k, indexes[m] = n and indexes[n] = -1
             *  (i,j,k) will form a group and (m,n) will form another group.
             *************************************************************************************/

            int[] indexes = Enumerable.Repeat((int)-1, elements.Count).ToArray();
            var candidatesPoints = elements.Select(candidatesPoint).ToList();

            // 1. Find nearest neighbours indexes
            Parallel.For(0, elements.Count, e =>
            {
                var pivot = elements[e];

                if (filterPivot(pivot))
                {
                    int index = pivotPoint(pivot).FindIndexNearest(candidatesPoints, distMeasure, out double dist);
                    var paired = elements[index];

                    if (filterFinal(pivot, paired) && dist < maxDistanceFunction(pivot, paired))
                    {
                        indexes[e] = index;
                    }
                }
            });

            // 2. Group indexes
            var groupedIndexes = GroupIndexes(indexes);

            return groupedIndexes;
        }

        /// <summary>
        /// Algorithm to group elements via transitive closure, using nearest neighbours and maximum distance.
        /// https://en.wikipedia.org/wiki/Transitive_closure
        /// </summary>
        /// <typeparam name="T">Letter, Word, TextLine, etc.</typeparam>
        /// <param name="elements">Array of elements to group.</param>
        /// <param name="distMeasure">The distance measure between two points.</param>
        /// <param name="maxDistanceFunction">The function that determines the maximum distance between two points in the same cluster.</param>
        /// <param name="pivotPoint">The pivot's point to use for pairing, e.g. BottomLeft, TopLeft.</param>
        /// <param name="candidatesPoint">The candidates' point to use for pairing, e.g. BottomLeft, TopLeft.</param>
        /// <param name="filterPivot">Filter to apply to the pivot point. If false, point will not be paired at all, e.g. is white space.</param>
        /// <param name="filterFinal">Filter to apply to both the pivot and the paired point. If false, point will not be paired at all, e.g. pivot and paired point have same font.</param>
        internal static IEnumerable<HashSet<int>> SimpleTransitiveClosure<T>(T[] elements,
            Func<PdfPoint, PdfPoint, double> distMeasure,
            Func<T, T, double> maxDistanceFunction,
            Func<T, PdfPoint> pivotPoint, Func<T, PdfPoint> candidatesPoint,
            Func<T, bool> filterPivot, Func<T, T, bool> filterFinal)
        {
            /*************************************************************************************
             * Algorithm steps
             * 1. Find nearest neighbours indexes (done in parallel)
             *  Iterate every point (pivot) and put its nearest neighbour's index in an array
             *  e.g. if nearest neighbour of point i is point j, then indexes[i] = j.
             *  Only conciders a neighbour if it is within the maximum distance. 
             *  If not within the maximum distance, index will be set to -1.
             *  Each element has only one connected neighbour.
             *  NB: Given the possible asymmetry in the relationship, it is possible 
             *  that if indexes[i] = j then indexes[j] != i.
             *  
             * 2. Group indexes
             *  Group indexes if share neighbours in common - Transitive closure
             *  e.g. if we have indexes[i] = j, indexes[j] = k, indexes[m] = n and indexes[n] = -1
             *  (i,j,k) will form a group and (m,n) will form another group.
             *************************************************************************************/

            int[] indexes = Enumerable.Repeat((int)-1, elements.Length).ToArray();
            var candidatesPoints = elements.Select(candidatesPoint).ToList();

            // 1. Find nearest neighbours indexes
            Parallel.For(0, elements.Length, e =>
            {
                var pivot = elements[e];

                if (filterPivot(pivot))
                {
                    int index = pivotPoint(pivot).FindIndexNearest(candidatesPoints, distMeasure, out double dist);
                    var paired = elements[index];

                    if (filterFinal(pivot, paired) && dist < maxDistanceFunction(pivot, paired))
                    {
                        indexes[e] = index;
                    }
                }
            });

            // 2. Group indexes
            var groupedIndexes = GroupIndexes(indexes);

            return groupedIndexes;
        }

        /// <summary>
        /// Algorithm to group elements via transitive closure, using nearest neighbours and maximum distance.
        /// https://en.wikipedia.org/wiki/Transitive_closure
        /// </summary>
        /// <typeparam name="T">Letter, Word, TextLine, etc.</typeparam>
        /// <param name="elements">Array of elements to group.</param>
        /// <param name="distMeasure">The distance measure between two lines.</param>
        /// <param name="maxDistanceFunction">The function that determines the maximum distance between two points in the same cluster.</param>
        /// <param name="pivotLine">The pivot's line to use for pairing.</param>
        /// <param name="candidatesLine">The candidates' line to use for pairing.</param>
        /// <param name="filterPivot">Filter to apply to the pivot point. If false, point will not be paired at all, e.g. is white space.</param>
        /// <param name="filterFinal">Filter to apply to both the pivot and the paired point. If false, point will not be paired at all, e.g. pivot and paired point have same font.</param>
        internal static IEnumerable<HashSet<int>> SimpleTransitiveClosure<T>(T[] elements,
            Func<PdfLine, PdfLine, double> distMeasure,
            Func<T, T, double> maxDistanceFunction,
            Func<T, PdfLine> pivotLine, Func<T, PdfLine> candidatesLine,
            Func<T, bool> filterPivot, Func<T, T, bool> filterFinal)
        {
            /*************************************************************************************
             * Algorithm steps
             * 1. Find nearest neighbours indexes (done in parallel)
             *  Iterate every point (pivot) and put its nearest neighbour's index in an array
             *  e.g. if nearest neighbour of point i is point j, then indexes[i] = j.
             *  Only conciders a neighbour if it is within the maximum distance. 
             *  If not within the maximum distance, index will be set to -1.
             *  Each element has only one connected neighbour.
             *  NB: Given the possible asymmetry in the relationship, it is possible 
             *  that if indexes[i] = j then indexes[j] != i.
             *  
             * 2. Group indexes
             *  Group indexes if share neighbours in common - Transitive closure
             *  e.g. if we have indexes[i] = j, indexes[j] = k, indexes[m] = n and indexes[n] = -1
             *  (i,j,k) will form a group and (m,n) will form another group.
             *************************************************************************************/

            int[] indexes = Enumerable.Repeat((int)-1, elements.Length).ToArray();
            var candidatesLines = elements.Select(x => candidatesLine(x)).ToList();

            // 1. Find nearest neighbours indexes
            Parallel.For(0, elements.Length, e =>
            {
                var pivot = elements[e];

                if (filterPivot(pivot))
                {
                    int index = pivotLine(pivot).FindIndexNearest(candidatesLines, distMeasure, out double dist);
                    var paired = elements[index];

                    if (filterFinal(pivot, paired) && dist < maxDistanceFunction(pivot, paired))
                    {
                        indexes[e] = index;
                    }
                }
            });

            // 2. Group indexes
            var groupedIndexes = GroupIndexes(indexes);

            return groupedIndexes;
        }

        /// <summary>
        /// Group elements via transitive closure. Each element has only one connected neighbour.
        /// https://en.wikipedia.org/wiki/Transitive_closure
        /// </summary>
        /// <param name="indexes">Array of paired elements index.</param>
        /// <returns></returns>
        private static List<HashSet<int>> GroupIndexes(int[] indexes)
        {
            int[][] adjacency = new int[indexes.Length][];
            for (int i = 0; i < indexes.Length; i++)
            {
                HashSet<int> matches = new HashSet<int>();
                for (int j = 0; j < indexes.Length; ++j)
                {
                    if (indexes[j] == i) matches.Add(j);
                }
                adjacency[i] = matches.ToArray();
            }

            List<HashSet<int>> groupedIndexes = new List<HashSet<int>>();
            bool[] isDone = new bool[indexes.Length];

            for (int p = 0; p < indexes.Length; p++)
            {
                if (isDone[p]) continue;

                LinkedList<int[]> L = new LinkedList<int[]>();
                HashSet<int> grouped = new HashSet<int>();
                L.AddLast(new[] { p, indexes[p] });

                while (L.Any())
                {
                    var current = L.First.Value;
                    L.RemoveFirst();
                    var current0 = current[0];
                    var current1 = current[1];

                    if (current0 != -1 && !isDone[current0])
                    {
                        var adjs = adjacency[current0];
                        foreach (var k in adjs)
                        {
                            if (isDone[k]) continue;
                            L.AddLast(new[] { k, current0 });
                        }

                        int current0P = indexes[current0];
                        if (current0P != -1)
                        {
                            var adjsP = adjacency[current0P];
                            foreach (var k in adjsP)
                            {
                                if (isDone[k]) continue;
                                L.AddLast(new[] { k, current0P });
                                isDone[k] = true;
                                grouped.Add(k);
                            }
                        }
                        else
                        {
                            L.AddLast(new[] { current0, current0P });
                            isDone[current0] = true;
                            grouped.Add(current0);
                        }
                    }

                    if (current1 != -1 && !isDone[current1])
                    {
                        var adjs = adjacency[current1];
                        foreach (var k in adjs)
                        {
                            if (isDone[k]) continue;
                            L.AddLast(new[] { k, current1 });
                        }

                        int current1P = indexes[current1];
                        if (current1P != -1)
                        {
                            var adjsP = adjacency[current1P];
                            foreach (var k in adjsP)
                            {
                                if (isDone[k]) continue;
                                L.AddLast(new[] { k, current1P });
                                isDone[k] = true;
                                grouped.Add(k);
                            }
                        }
                        else
                        {
                            L.AddLast(new[] { current1, current1P });
                            isDone[current1] = true;
                            grouped.Add(current1);
                        }
                    }
                }
                groupedIndexes.Add(grouped);
            }

            return groupedIndexes;
        }
    }
}
