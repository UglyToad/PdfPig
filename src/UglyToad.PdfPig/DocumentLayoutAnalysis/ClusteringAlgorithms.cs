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
        internal static IEnumerable<HashSet<int>> ClusterNearestNeighbours<T>(List<T> elements,
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
             *  Group indexes if share neighbours in common - Depth-first search
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
                    int index = pivot.FindIndexNearest(elements, candidatesPoint, pivotPoint, distMeasure, out double dist);

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
        internal static IEnumerable<HashSet<int>> ClusterNearestNeighbours<T>(T[] elements,
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
             *  Group indexes if share neighbours in common - Depth-first search
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
                    int index = pivot.FindIndexNearest(elements, candidatesPoint, pivotPoint, distMeasure, out double dist);

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
        internal static IEnumerable<HashSet<int>> ClusterNearestNeighbours<T>(T[] elements,
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
             *  Group indexes if share neighbours in common - Depth-first search
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
                    int index = pivot.FindIndexNearest(elements, candidatesLine, pivotLine, distMeasure, out double dist);

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
            var groupedIndexes = GroupIndexes(indexes);

            return groupedIndexes;
        }

        /// <summary>
        /// Group elements using Depth-first search.
        /// <para>https://en.wikipedia.org/wiki/Depth-first_search</para>
        /// </summary>
        /// <param name="edges">The graph. edges[i] = j indicates that there is an edge between i and j.</param>
        /// <returns>A List of HashSets containing containing the grouped indexes.</returns>
        internal static List<HashSet<int>> GroupIndexes(int[] edges)
        {
            int[][] adjacency = new int[edges.Length][];
            for (int i = 0; i < edges.Length; i++)
            {
                HashSet<int> matches = new HashSet<int>();
                if (edges[i] != -1) matches.Add(edges[i]);
                for (int j = 0; j < edges.Length; j++)
                {
                    if (edges[j] == i) matches.Add(j);
                }
                adjacency[i] = matches.ToArray();
            }

            List<HashSet<int>> groupedIndexes = new List<HashSet<int>>();
            bool[] isDone = new bool[edges.Length];

            for (int p = 0; p < edges.Length; p++)
            {
                if (isDone[p]) continue;
                groupedIndexes.Add(DfsIterative(p, adjacency, ref isDone));
            }
            return groupedIndexes;
        }

        /// <summary>
        /// Group elements using Depth-first search.
        /// <para>https://en.wikipedia.org/wiki/Depth-first_search</para>
        /// </summary>
        /// <param name="edges">The graph. edges[i] = [j, k, l, ...] indicates that there is an edge between i and each element j, k, l, ...</param>
        /// <returns>A List of HashSets containing containing the grouped indexes.</returns>
        internal static List<HashSet<int>> GroupIndexes(int[][] edges)
        {
            int[][] adjacency = new int[edges.Length][];
            for (int i = 0; i < edges.Length; i++)
            {
                HashSet<int> matches = new HashSet<int>();
                for (int j = 0; j < edges[i].Length; j++)
                {
                    if (edges[i][j] != -1) matches.Add(edges[i][j]);
                }

                for (int j = 0; j < edges.Length; j++)
                {
                    for (int k = 0; k < edges[j].Length; k++)
                    {
                        if (edges[j][k] == i) matches.Add(j);
                    }
                }
                adjacency[i] = matches.ToArray();
            }

            List<HashSet<int>> groupedIndexes = new List<HashSet<int>>();
            bool[] isDone = new bool[edges.Length];

            for (int p = 0; p < edges.Length; p++)
            {
                if (isDone[p]) continue;
                groupedIndexes.Add(DfsIterative(p, adjacency, ref isDone));
            }
            return groupedIndexes;
        }

        /// <summary>
        /// Depth-first search
        /// <para>https://en.wikipedia.org/wiki/Depth-first_search</para>
        /// </summary>
        private static HashSet<int> DfsIterative(int c, int[][] adj, ref bool[] isDone)
        {
            HashSet<int> group = new HashSet<int>();
            Stack<int> S = new Stack<int>();
            S.Push(c);

            while (S.Any())
            {
                var v = S.Pop();
                if (!isDone[v])
                {
                    group.Add(v);
                    isDone[v] = true;
                    foreach (var w in adj[v])
                    {
                        S.Push(w);
                    }
                }
            }
            return group;
        }
    }
}
