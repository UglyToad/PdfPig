namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Clustering Algorithms.
    /// </summary>
    public static class Clustering
    {
        /// <summary>
        /// Algorithm to group elements using nearest neighbours.
        /// <para>Uses the nearest neighbour as candidate.</para>
        /// </summary>
        /// <typeparam name="T">Letter, Word, TextLine, etc.</typeparam>
        /// <param name="elements">Elements to group.</param>
        /// <param name="distMeasure">The distance measure between two points.</param>
        /// <param name="maxDistanceFunction">The function that determines the maximum distance between two points in the same cluster.</param>
        /// <param name="pivotPoint">The pivot's point to use for pairing, e.g. BottomLeft, TopLeft.</param>
        /// <param name="candidatesPoint">The candidates' point to use for pairing, e.g. BottomLeft, TopLeft.</param>
        /// <param name="filterPivot">Filter to apply to the pivot point. If false, point will not be paired at all, e.g. is white space.</param>
        /// <param name="filterFinal">Filter to apply to both the pivot and the paired point. If false, point will not be paired at all, e.g. pivot and paired point have same font.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled.
        /// <para>A positive property value limits the number of concurrent operations to the set value.
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        public static IEnumerable<IReadOnlyList<T>> NearestNeighbours<T>(IReadOnlyList<T> elements,
            Func<PdfPoint, PdfPoint, double> distMeasure,
            Func<T, T, double> maxDistanceFunction,
            Func<T, PdfPoint> pivotPoint, Func<T, PdfPoint> candidatesPoint,
            Func<T, bool> filterPivot, Func<T, T, bool> filterFinal,
            int maxDegreeOfParallelism)
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
             *  Group indexes if share neighbours in common - Depth-first search
             *  e.g. if we have indexes[i] = j, indexes[j] = k, indexes[m] = n and indexes[n] = -1
             *  (i,j,k) will form a group and (m,n) will form another group.
             *************************************************************************************/

            int[] indexes = new int[elements.Count];
#if NET6_0_OR_GREATER
            Array.Fill(indexes, -1);
#else
            for (int k = 0; k < indexes.Length; k++)
            {
                indexes[k] = -1;
            }
#endif
            KdTree<T> kdTree = new KdTree<T>(elements, candidatesPoint);

            ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            // 1. Find nearest neighbours indexes
            Parallel.For(0, elements.Count, parallelOptions, e =>
            {
                var pivot = elements[e];

                if (filterPivot(pivot))
                {
                    var paired = kdTree.FindNearestNeighbour(pivot, pivotPoint, distMeasure, out int index, out double dist);

                    if (index != -1 && filterFinal(pivot, paired) && dist < maxDistanceFunction(pivot, paired))
                    {
                        indexes[e] = index;
                    }
                }
            });

            // 2. Group indexes
            foreach (var group in GroupIndexes(indexes))
            {
                yield return group.Select(i => elements[i]).ToList();
            }
        }

        /// <summary>
        /// Algorithm to group elements using nearest neighbours.
        /// <para>Uses the k-nearest neighbours as candidates.</para>
        /// </summary>
        /// <typeparam name="T">Letter, Word, TextLine, etc.</typeparam>
        /// <param name="elements">Elements to group.</param>
        /// <param name="k">The k-nearest neighbours to consider as candidates.</param>
        /// <param name="distMeasure">The distance measure between two points.</param>
        /// <param name="maxDistanceFunction">The function that determines the maximum distance between two points in the same cluster.</param>
        /// <param name="pivotPoint">The pivot's point to use for pairing, e.g. BottomLeft, TopLeft.</param>
        /// <param name="candidatesPoint">The candidates' point to use for pairing, e.g. BottomLeft, TopLeft.</param>
        /// <param name="filterPivot">Filter to apply to the pivot point. If false, point will not be paired at all, e.g. is white space.</param>
        /// <param name="filterFinal">Filter to apply to both the pivot and the paired point. If false, point will not be paired at all, e.g. pivot and paired point have same font.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled.
        /// <para>A positive property value limits the number of concurrent operations to the set value.
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        public static IEnumerable<IReadOnlyList<T>> NearestNeighbours<T>(IReadOnlyList<T> elements, int k,
            Func<PdfPoint, PdfPoint, double> distMeasure,
            Func<T, T, double> maxDistanceFunction,
            Func<T, PdfPoint> pivotPoint, Func<T, PdfPoint> candidatesPoint,
            Func<T, bool> filterPivot, Func<T, T, bool> filterFinal,
            int maxDegreeOfParallelism)
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
             *  Group indexes if share neighbours in common - Depth-first search
             *  e.g. if we have indexes[i] = j, indexes[j] = k, indexes[m] = n and indexes[n] = -1
             *  (i,j,k) will form a group and (m,n) will form another group.
             *************************************************************************************/

            int[] indexes = new int[elements.Count];
#if NET6_0_OR_GREATER
            Array.Fill(indexes, -1);
#else
            for (int l = 0; l < indexes.Length; l++)
            {
                indexes[l] = -1;
            }
#endif
            KdTree<T> kdTree = new KdTree<T>(elements, candidatesPoint);

            ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            // 1. Find nearest neighbours indexes
            Parallel.For(0, elements.Count, parallelOptions, e =>
            {
                var pivot = elements[e];

                if (filterPivot(pivot))
                {
                    foreach (var c in kdTree.FindNearestNeighbours(pivot, k, pivotPoint, distMeasure))
                    {
                        if (filterFinal(pivot, c.Item1) && c.Item3 < maxDistanceFunction(pivot, c.Item1))
                        {
                            indexes[e] = c.Item2;
                            break;
                        }
                    }
                }
            });

            // 2. Group indexes
            foreach (var group in GroupIndexes(indexes))
            {
                yield return group.Select(i => elements[i]).ToList();
            }
        }

        /// <summary>
        /// Algorithm to group elements using nearest neighbours.
        /// </summary>
        /// <typeparam name="T">Letter, Word, TextLine, etc.</typeparam>
        /// <param name="elements">Array of elements to group.</param>
        /// <param name="distMeasure">The distance measure between two lines.</param>
        /// <param name="maxDistanceFunction">The function that determines the maximum distance between two points in the same cluster.</param>
        /// <param name="pivotLine">The pivot's line to use for pairing.</param>
        /// <param name="candidatesLine">The candidates' line to use for pairing.</param>
        /// <param name="filterPivot">Filter to apply to the pivot point. If false, point will not be paired at all, e.g. is white space.</param>
        /// <param name="filterFinal">Filter to apply to both the pivot and the paired point. If false, point will not be paired at all, e.g. pivot and paired point have same font.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled.
        /// <para>A positive property value limits the number of concurrent operations to the set value.
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        public static IEnumerable<IReadOnlyList<T>> NearestNeighbours<T>(IReadOnlyList<T> elements,
            Func<PdfLine, PdfLine, double> distMeasure,
            Func<T, T, double> maxDistanceFunction,
            Func<T, PdfLine> pivotLine, Func<T, PdfLine> candidatesLine,
            Func<T, bool> filterPivot, Func<T, T, bool> filterFinal,
            int maxDegreeOfParallelism)
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
             *  Group indexes if share neighbours in common - Depth-first search
             *  e.g. if we have indexes[i] = j, indexes[j] = k, indexes[m] = n and indexes[n] = -1
             *  (i,j,k) will form a group and (m,n) will form another group.
             *************************************************************************************/

            int[] indexes = new int[elements.Count];
#if NET6_0_OR_GREATER
            Array.Fill(indexes, -1);
#else
            for (int k = 0; k < indexes.Length; k++)
            {
                indexes[k] = -1;
            }
#endif

            ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            // 1. Find nearest neighbours indexes
            Parallel.For(0, elements.Count, parallelOptions, e =>
            {
                var pivot = elements[e];

                if (filterPivot(pivot))
                {
                    int index = Distances.FindIndexNearest(pivot, elements, pivotLine, candidatesLine,  distMeasure, out double dist);

                    if (index != -1)
                    {
                        var paired = elements[index];
                        if (filterFinal(pivot, paired) && dist < maxDistanceFunction(pivot, paired))
                        {
                            indexes[e] = index;
                        }
                    }
                }
            });

            // 2. Group indexes
            foreach (var group in GroupIndexes(indexes))
            {
                yield return group.Select(i => elements[i]).ToList();
            }
        }
        
        internal static List<List<int>> GroupIndexes(int[] edges)
        {
            // Improved thanks to https://github.com/UglyToad/PdfPig/issues/1178
            var adjacency = new List<int>[edges.Length];
            for (int i = 0; i < edges.Length; i++)
            {
                adjacency[i] = new List<int>();
            }

            // one pass O(n) 
            for (int i = 0; i < edges.Length; i++)
            {
                int j = edges[i];
                if (j != -1)
                {
                    // i <-> j
                    adjacency[i].Add(j);
                    adjacency[j].Add(i);
                }
            }

            List<List<int>> groupedIndexes = new List<List<int>>();
            bool[] isDone = new bool[edges.Length];

            for (int p = 0; p < edges.Length; p++)
            {
                if (isDone[p])
                {
                    continue;
                }
                groupedIndexes.Add(DfsIterative(p, adjacency, ref isDone));
            }
            return groupedIndexes;
        }

        /// <summary>
        /// Depth-first search
        /// <para>https://en.wikipedia.org/wiki/Depth-first_search</para>
        /// </summary>
        private static List<int> DfsIterative(int s, List<int>[] adj, ref bool[] isDone)
        {
            List<int> group = new List<int>();
            Stack<int> S = new Stack<int>(4);
            S.Push(s);

            isDone[s] = true;
            while (S.Count > 0)
            {
                var u = S.Pop();
                group.Add(u);

#if NET
                var currentAdj = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(adj[u]);
                int count = currentAdj.Length;
#else
                var currentAdj = adj[u];
                int count = currentAdj.Count;
#endif
                for (int i = 0; i < count; ++i)
                {
                    var v = currentAdj[i];
                    ref bool done = ref isDone[v];
                    if (!done)
                    {
                        S.Push(v);
                        done = true;
                    }
                }
            }
            return group;
        }
    }
}
