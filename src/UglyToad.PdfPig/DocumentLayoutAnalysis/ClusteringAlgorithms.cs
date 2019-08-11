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
             *  NB: Given the possible asymmetry in the relationship, it is possible 
             *  that if indexes[i] = j then indexes[j] != i.
             *  
             * 2. Group indexes
             *  Group indexes if share neighbours in common - Transitive closure
             *  e.g. if we have indexes[i] = j, indexes[j] = k, indexes[m] = n and indexes[n] = -1
             *  (i,j,k) will form a group and (m,n) will form another group.
             *  
             * 3. Merge groups that have indexes in common - If any
             *  If there are group with indexes in common, merge them.
             *  (Could be improved and put in step 2)
             *************************************************************************************/

            int[] indexes = Enumerable.Repeat((int)-1, elements.Length).ToArray();
            var candidatesPoints = elements.Select(x => candidatesPoint(x)).ToList();

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
            // 3. Merge groups that have indexes in common
            var groupedIndexes = GroupMergeIndexes(indexes);

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
             *  NB: Given the possible asymmetry in the relationship, it is possible 
             *  that if indexes[i] = j then indexes[j] != i.
             *  
             * 2. Group indexes
             *  Group indexes if share neighbours in common - Transitive closure
             *  e.g. if we have indexes[i] = j, indexes[j] = k, indexes[m] = n and indexes[n] = -1
             *  (i,j,k) will form a group and (m,n) will form another group.
             *  
             * 3. Merge groups that have indexes in common - If any
             *  If there are group with indexes in common, merge them.
             *  (Could be improved and put in step 2)
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
            // 3. Merge groups that have indexes in common
            var groupedIndexes = GroupMergeIndexes(indexes);

            return groupedIndexes;
        }

        /// <summary>
        /// Group elements via transitive closure.
        /// https://en.wikipedia.org/wiki/Transitive_closure
        /// </summary>
        /// <param name="indexes">Array of paired elements index.</param>
        /// <returns></returns>
        internal static List<HashSet<int>> GroupMergeIndexes(int[] indexes)
        {
            // 2. Group indexes
            List<HashSet<int>> groupedIndexes = new List<HashSet<int>>();
            HashSet<int> indexDone = new HashSet<int>();

            for (int e = 0; e < indexes.Length; e++)
            {
                int index = indexes[e];

                if (index == -1) // This element is not connected
                {
                    // Check if another element's index is connected to this element (nb: distance measure is asymmetric)
                    if (!indexes.Contains(e))
                    {
                        // If no other element is connected to this element, add it as a standalone element
                        groupedIndexes.Add(new HashSet<int>() { e });
                        indexDone.Add(e);
                    }
                    continue;
                }

                bool isDoneC = indexDone.Contains(e);
                bool isDoneI = indexDone.Contains(index);
                if (isDoneC || isDoneI)
                {
                    if (isDoneC && !isDoneI)
                    {
                        foreach (var pair in groupedIndexes.Where(x => x.Contains(e)))
                        {
                            pair.Add(index);
                        }
                        indexDone.Add(index);
                    }
                    else if (!isDoneC && isDoneI)
                    {
                        foreach (var pair in groupedIndexes.Where(x => x.Contains(index)))
                        {
                            pair.Add(e);
                        }
                        indexDone.Add(e);
                    }
                    else // isDoneC && isDoneI
                    {
                        foreach (var pair in groupedIndexes.Where(x => x.Contains(index)))
                        {
                            if (!pair.Contains(e)) pair.Add(e);
                        }

                        foreach (var pair in groupedIndexes.Where(x => x.Contains(e)))
                        {
                            if (!pair.Contains(index)) pair.Add(index);
                        }
                    }
                }
                else
                {
                    groupedIndexes.Add(new HashSet<int>() { e, index });
                    indexDone.Add(e);
                    indexDone.Add(index);
                }
            }

            // Check that all elements are done
            if (indexes.Length != indexDone.Count)
            {
                throw new Exception("ClusteringAlgorithms.GetNNGroupedIndexes(): Some elements were not done.");
            }

            // 3. Merge groups that have indexes in common
            // Check if duplicates (if duplicates, then same index in different groups)
            if (indexDone.Count != groupedIndexes.SelectMany(x => x).Count())
            {
                for (int e = 0; e < indexes.Length; e++)
                {
                    List<HashSet<int>> candidates = groupedIndexes.Where(x => x.Contains(e)).ToList();
                    int count = candidates.Count();
                    if (count < 2) continue; // Only one group with this index

                    HashSet<int> merged = candidates.First();
                    groupedIndexes.Remove(merged);
                    for (int i = 1; i < count; i++)
                    {
                        var current = candidates.ElementAt(i);
                        merged.UnionWith(current);
                        groupedIndexes.Remove(current);
                    }
                    groupedIndexes.Add(merged);
                }
            }
            return groupedIndexes;
        }
    }
}
