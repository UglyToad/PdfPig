namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using Core;
    using System;
    using System.Buffers;
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
        /// <para>Within a group, each element is placed before its nearest neighbour.</para>
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
            foreach (var group in NearestNeighbourGroups(elements, distMeasure, maxDistanceFunction,
                         pivotPoint, candidatesPoint, filterPivot, filterFinal, maxDegreeOfParallelism))
            {
                yield return group;
            }
        }

        /// <summary>
        /// Eager version of <see cref="NearestNeighbours{T}(IReadOnlyList{T}, Func{PdfPoint, PdfPoint, double}, Func{T, T, double}, Func{T, PdfPoint}, Func{T, PdfPoint}, Func{T, bool}, Func{T, T, bool}, int)"/>.
        /// </summary>
        internal static List<T[]> NearestNeighbourGroups<T>(IReadOnlyList<T> elements,
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
             *  Group indexes if share neighbours in common, ordered by following the links
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
            return GroupByLinks(indexes, elements);
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

        /// <summary>
        /// Group the indexes connected by <paramref name="edges"/>, where <c>edges[i]</c> is the index of the element
        /// following <c>i</c> (or -1 if none).
        /// <para>Within a group, every element is placed before the element it is connected to, e.g. if
        /// <c>edges[i] = j</c> then <c>i</c> comes before <c>j</c>. When several elements are connected to the same
        /// element, they are placed by ascending index. If a group contains a cycle, the cycle is cut after its highest index.</para>
        /// <para>Groups are returned by ascending lowest index.</para>
        /// </summary>
        internal static List<T[]> GroupByLinks<T>(int[] edges, IReadOnlyList<T> elements)
        {
            int n = edges.Length;
            var intPool = ArrayPool<int>.Shared;

            // Reverse edges (i.e. predecessors) in CSR form: the predecessors of j are
            // predecessors[predecessorsStart[j]..predecessorsStart[j + 1]], by ascending index.
            // Counts are stored 2 slots ahead so that, once filled, the offsets are already in place.
            int[] predecessorsStart = intPool.Rent(n + 2);
            Array.Clear(predecessorsStart, 0, n + 2);
            int predecessorsCount = 0;
            for (int i = 0; i < n; i++)
            {
                int j = edges[i];
                if (j != -1)
                {
                    predecessorsStart[j + 2]++;
                    predecessorsCount++;
                }
            }

            for (int j = 2; j < n + 2; j++)
            {
                predecessorsStart[j] += predecessorsStart[j - 1];
            }

            // predecessorsStart[j + 1] is the start of j's range, and becomes its end once filled.
            int[] predecessors = intPool.Rent(predecessorsCount);
            for (int i = 0; i < n; i++)
            {
                int j = edges[i];
                if (j != -1)
                {
                    predecessors[predecessorsStart[j + 1]++] = i;
                }
            }

            bool[] isDone = ArrayPool<bool>.Shared.Rent(n);
            Array.Clear(isDone, 0, n);

            // The group's indexes are written from the start of the buffer, and the stack grows down from
            // its end. They never overlap as each index is written at most once to either of them.
            int[] buffer = intPool.Rent(n);

            var groups = new List<T[]>();
            for (int p = 0; p < n; p++)
            {
                if (isDone[p])
                {
                    continue;
                }

                // Every element of the group leads to the same last element, from which the whole
                // group is reachable through the predecessors.
                int last = FindLast(p, edges);

                // Pre-order traversal of the predecessors, visiting the highest index first. Reversed,
                // this gives each element's predecessors (by ascending index) before the element itself.
                int count = 0;
                int top = n;
                buffer[--top] = last;
                isDone[last] = true;
                while (top < n)
                {
                    int u = buffer[top++];
                    buffer[count++] = u;

                    for (int k = predecessorsStart[u]; k < predecessorsStart[u + 1]; k++)
                    {
                        int v = predecessors[k];
                        if (!isDone[v])
                        {
                            // Only the edge that closes a cycle leads to a done element
                            isDone[v] = true;
                            buffer[--top] = v;
                        }
                    }
                }

                var group = new T[count];
                for (int k = 0; k < count; k++)
                {
                    group[k] = elements[buffer[count - 1 - k]];
                }

                groups.Add(group);
            }

            intPool.Return(predecessorsStart);
            intPool.Return(predecessors);
            intPool.Return(buffer);
            ArrayPool<bool>.Shared.Return(isDone);

            return groups;
        }

        /// <summary>
        /// Follow the edges from <paramref name="start"/> to the last element: the element without edge,
        /// or the highest index of the cycle if the edges loop (Floyd's cycle detection).
        /// </summary>
        private static int FindLast(int start, int[] edges)
        {
            int slow = start;
            int fast = start;
            while (true)
            {
                if (edges[fast] == -1)
                {
                    return fast;
                }

                fast = edges[fast];
                if (edges[fast] == -1)
                {
                    return fast;
                }

                fast = edges[fast];
                slow = edges[slow];
                if (slow == fast)
                {
                    break;
                }
            }

            // slow is in the cycle
            int last = slow;
            for (int i = edges[slow]; i != slow; i = edges[i])
            {
                if (i > last)
                {
                    last = i;
                }
            }

            return last;
        }
    }
}
