namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Core;

    // for kd-tree with line segments, see https://stackoverflow.com/questions/14376679/how-to-represent-line-segments-in-kd-tree 

    /// <summary>
    /// K-D tree data structure of <see cref="PdfPoint"/>.
    /// </summary>
    public class KdTree : KdTree<PdfPoint>
    {
        /// <summary>
        /// K-D tree data structure of <see cref="PdfPoint"/>.
        /// </summary>
        /// <param name="points">The points used to build the tree.</param>
        public KdTree(IReadOnlyList<PdfPoint> points) : base(points, p => p)
        { }

        /// <summary>
        /// Get the nearest neighbour to the pivot point.
        /// Only returns 1 neighbour, even if equidistant points are found.
        /// </summary>
        /// <param name="pivot">The point for which to find the nearest neighbour.</param>
        /// <param name="distanceMeasure">The distance measure used, e.g. the Euclidian distance.</param>
        /// <param name="index">The nearest neighbour's index (returns -1 if not found).</param>
        /// <param name="distance">The distance between the pivot and the nearest neighbour (returns <see cref="double.NaN"/> if not found).</param>
        /// <returns>The nearest neighbour's point.</returns>
        public PdfPoint FindNearestNeighbour(PdfPoint pivot, Func<PdfPoint, PdfPoint, double> distanceMeasure, out int index, out double distance)
        {
            return FindNearestNeighbour(pivot, p => p, distanceMeasure, out index, out distance);
        }

        /// <summary>
        /// Get the k nearest neighbours to the pivot point.
        /// Might return more than k neighbours if points are equidistant.
        /// <para>Use <see cref="FindNearestNeighbour(PdfPoint, Func{PdfPoint, PdfPoint, double}, out int, out double)"/> if only looking for the (single) closest point.</para>
        /// </summary>
        /// <param name="pivot">The point for which to find the nearest neighbour.</param>
        /// <param name="k">The number of neighbours to return. Might return more than k neighbours if points are equidistant.</param>
        /// <param name="distanceMeasure">The distance measure used, e.g. the Euclidian distance.</param>
        /// <returns>Returns a list of tuples of the k nearest neighbours. Tuples are (element, index, distance).</returns>
        public IReadOnlyList<(PdfPoint, int, double)> FindNearestNeighbours(PdfPoint pivot, int k, Func<PdfPoint, PdfPoint, double> distanceMeasure)
        {
            return FindNearestNeighbours(pivot, k, p => p, distanceMeasure);
        }
    }

    /// <summary>
    /// K-D tree data structure.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KdTree<T>
    {
        /// <summary>
        /// The root of the tree.
        /// </summary>
        public readonly KdTreeNode<T> Root;

        /// <summary>
        /// Number of elements in the tree.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// K-D tree data structure.
        /// </summary>
        /// <param name="elements">The elements used to build the tree.</param>
        /// <param name="elementsPointFunc">The function that converts the candidate elements into a <see cref="PdfPoint"/>.</param>
        public KdTree(IReadOnlyList<T> elements, Func<T, PdfPoint> elementsPointFunc)
        {
            if (elements == null || elements.Count == 0)
            {
                throw new ArgumentException("KdTree(): candidates cannot be null or empty.", nameof(elements));
            }

            Count = elements.Count;

            KdTreeElement<T>[] array = new KdTreeElement<T>[Count];

            for (int i = 0; i < Count; i++)
            {
                var el = elements[i];
                array[i] = new KdTreeElement<T>(i, elementsPointFunc(el), el);
            }

#if NET6_0_OR_GREATER
            Root = BuildTree(new Span<KdTreeElement<T>>(array));
#else
            Root = BuildTree(new ArraySegment<KdTreeElement<T>>(array));
#endif
        }

#if NET6_0_OR_GREATER
        private KdTreeNode<T> BuildTree(Span<KdTreeElement<T>> P, int depth = 0)
        {
            if (P.Length == 0)
            {
                return null;
            }

            if (P.Length == 1)
            {
                return new KdTreeLeaf<T>(P[0], depth);
            }

            if (depth % 2 == 0)
            {
                P.Sort((p0, p1) => p0.Value.X.CompareTo(p1.Value.X));
            }
            else
            {
                P.Sort((p0, p1) => p0.Value.Y.CompareTo(p1.Value.Y));
            }

            if (P.Length == 2)
            {
                return new KdTreeNode<T>(new KdTreeLeaf<T>(P[0], depth + 1), null, P[1], depth);
            }

            int median = P.Length / 2;

            KdTreeNode<T> vLeft = BuildTree(P.Slice(0, median), depth + 1);
            KdTreeNode<T> vRight = BuildTree(P.Slice(median + 1), depth + 1);

            return new KdTreeNode<T>(vLeft, vRight, P[median], depth);
        }
#else
        private sealed class KdTreeComparerY : IComparer<KdTreeElement<T>>
        {
            public static readonly KdTreeComparerY Shared = new KdTreeComparerY();

            public int Compare(KdTreeElement<T> p0, KdTreeElement<T> p1)
            {
                return p0.Value.Y.CompareTo(p1.Value.Y);
            }
        }

        private sealed class KdTreeComparerX : IComparer<KdTreeElement<T>>
        {
            public static readonly KdTreeComparerX Shared = new KdTreeComparerX();

            public int Compare(KdTreeElement<T> p0, KdTreeElement<T> p1)
            {
                return p0.Value.X.CompareTo(p1.Value.X);
            }
        }

        private KdTreeNode<T> BuildTree(ArraySegment<KdTreeElement<T>> P, int depth = 0)
        {
            if (P.Count == 0)
            {
                return null;
            }

            if (P.Count == 1)
            {
                return new KdTreeLeaf<T>(P.GetAt(0), depth);
            }

            if (depth % 2 == 0)
            {
                P.Sort(KdTreeComparerX.Shared);
            }
            else
            {
                P.Sort(KdTreeComparerY.Shared);
            }
            
            if (P.Count == 2)
            {
                return new KdTreeNode<T>(new KdTreeLeaf<T>(P.GetAt(0), depth + 1), null, P.GetAt(1), depth);
            }

            int median = P.Count / 2;

            KdTreeNode<T> vLeft = BuildTree(P.Take(median), depth + 1);
            KdTreeNode<T> vRight = BuildTree(P.Skip(median + 1), depth + 1);

            return new KdTreeNode<T>(vLeft, vRight, P.GetAt(median), depth);
        }
#endif

        #region NN
        /// <summary>
        /// Get the nearest neighbour to the pivot element.
        /// Only returns 1 neighbour, even if equidistant points are found.
        /// </summary>
        /// <param name="pivot">The element for which to find the nearest neighbour.</param>
        /// <param name="pivotPointFunc">The function that converts the pivot element into a <see cref="PdfPoint"/>.</param>
        /// <param name="distanceMeasure">The distance measure used, e.g. the Euclidian distance.</param>
        /// <param name="index">The nearest neighbour's index (returns -1 if not found).</param>
        /// <param name="distance">The distance between the pivot and the nearest neighbour (returns <see cref="double.NaN"/> if not found).</param>
        /// <returns>The nearest neighbour's element.</returns>
        public T FindNearestNeighbour(T pivot, Func<T, PdfPoint> pivotPointFunc, Func<PdfPoint, PdfPoint, double> distanceMeasure, out int index, out double distance)
        {
            var pivotPoint = pivotPointFunc(pivot);
            var result = FindNearestNeighbour(Root, pivot, pivotPoint, distanceMeasure);
            index = result.Item1 != null ? result.Item1.Index : -1;
            distance = result.Item2 ?? double.NaN;
            return result.Item1 != null ? result.Item1.Element : default;
        }

        private static (KdTreeNode<T>, double?) FindNearestNeighbour(KdTreeNode<T> node, T pivot, PdfPoint pivotPoint, Func<PdfPoint, PdfPoint, double> distance)
        {
            if (node == null)
            {
                return (null, null);
            }
            else if (node.IsLeaf)
            {
                if (node.Element.Equals(pivot))
                {
                    return (null, null);
                }
                return (node, distance(node.Value, pivotPoint));
            }
            else
            {
                var currentNearestNode = node;
                var currentDistance = distance(node.Value, pivotPoint);

                KdTreeNode<T> newNode = null;
                double? newDist = null;

                var pointValue = node.IsAxisCutX ? pivotPoint.X : pivotPoint.Y;

                if (pointValue < node.L)
                {
                    // start left
                    (newNode, newDist) = FindNearestNeighbour(node.LeftChild, pivot, pivotPoint, distance);

                    if (newDist.HasValue && newDist <= currentDistance && !newNode.Element.Equals(pivot))
                    {
                        currentDistance = newDist.Value;
                        currentNearestNode = newNode;
                    }

                    if (node.RightChild != null && pointValue + currentDistance >= node.L)
                    {
                        (newNode, newDist) = FindNearestNeighbour(node.RightChild, pivot, pivotPoint, distance);
                    }
                }
                else
                {
                    // start right
                    (newNode, newDist) = FindNearestNeighbour(node.RightChild, pivot, pivotPoint, distance);

                    if (newDist.HasValue && newDist <= currentDistance && !newNode.Element.Equals(pivot))
                    {
                        currentDistance = newDist.Value;
                        currentNearestNode = newNode;
                    }

                    if (node.LeftChild != null && pointValue - currentDistance <= node.L)
                    {
                        (newNode, newDist) = FindNearestNeighbour(node.LeftChild, pivot, pivotPoint, distance);
                    }
                }

                if (newDist.HasValue && newDist <= currentDistance && !newNode.Element.Equals(pivot))
                {
                    currentDistance = newDist.Value;
                    currentNearestNode = newNode;
                }

                return (currentNearestNode, currentDistance);
            }
        }
        #endregion

        #region k-NN
        /// <summary>
        /// Get the k nearest neighbours to the pivot element.
        /// Might return more than k neighbours if points are equidistant.
        /// <para>Use <see cref="FindNearestNeighbour(T, Func{T, PdfPoint}, Func{PdfPoint, PdfPoint, double}, out int, out double)"/> if only looking for the (single) closest point.</para>
        /// </summary>
        /// <param name="pivot">The element for which to find the k nearest neighbours.</param>
        /// <param name="k">The number of neighbours to return. Might return more than k neighbours if points are equidistant.</param>
        /// <param name="pivotPointFunc">The function that converts the pivot element into a <see cref="PdfPoint"/>.</param>
        /// <param name="distanceMeasure">The distance measure used, e.g. the Euclidian distance.</param>
        /// <returns>Returns a list of tuples of the k nearest neighbours. Tuples are (element, index, distance).</returns>
        public IReadOnlyList<(T, int, double)> FindNearestNeighbours(T pivot, int k, Func<T, PdfPoint> pivotPointFunc, Func<PdfPoint, PdfPoint, double> distanceMeasure)
        {
            var pivotPoint = pivotPointFunc(pivot);
            var kdTreeNodes = new KNearestNeighboursQueue(k);
            FindNearestNeighbours(Root, pivot, k, pivotPoint, distanceMeasure, kdTreeNodes);

            var results = new List<(T, int, double)>();
            for (int i = 0; i < kdTreeNodes.Count; i++)
            {
                double dist = kdTreeNodes.Keys[i];
                foreach (var e in kdTreeNodes.Values[i])
                {
                    results.Add((e.Element, e.Index, dist));
                }
            }
            return results;
        }

        private static (KdTreeNode<T>, double) FindNearestNeighbours(KdTreeNode<T> node, T pivot, int k,
            PdfPoint pivotPoint, Func<PdfPoint, PdfPoint, double> distance, KNearestNeighboursQueue queue)
        {
            if (node == null)
            {
                return (null, double.NaN);
            }
            else if (node.IsLeaf)
            {
                if (node.Element.Equals(pivot))
                {
                    return (null, double.NaN);
                }

                var currentDistance = distance(node.Value, pivotPoint);
                var currentNearestNode = node;

                if (!queue.IsFull || currentDistance <= queue.LastDistance)
                {
                    queue.Add(currentDistance, currentNearestNode);
                    currentDistance = queue.LastDistance;
                    currentNearestNode = queue.LastElement;
                }

                return (currentNearestNode, currentDistance);
            }
            else
            {
                var currentNearestNode = node;
                var currentDistance = distance(node.Value, pivotPoint);
                if ((!queue.IsFull || currentDistance <= queue.LastDistance) && !node.Element.Equals(pivot))
                {
                    queue.Add(currentDistance, currentNearestNode);
                    currentDistance = queue.LastDistance;
                    currentNearestNode = queue.LastElement;
                }

                KdTreeNode<T> newNode = null;
                double newDist = double.NaN;

                var pointValue = node.IsAxisCutX ? pivotPoint.X : pivotPoint.Y;

                if (pointValue < node.L)
                {
                    // start left
                    (newNode, newDist) = FindNearestNeighbours(node.LeftChild, pivot, k, pivotPoint, distance, queue);

                    if (!double.IsNaN(newDist) && newDist <= currentDistance && !newNode.Element.Equals(pivot))
                    {
                        queue.Add(newDist, newNode);
                        currentDistance = queue.LastDistance;
                        currentNearestNode = queue.LastElement;
                    }

                    if (node.RightChild != null && pointValue + currentDistance >= node.L)
                    {
                        (newNode, newDist) = FindNearestNeighbours(node.RightChild, pivot, k, pivotPoint, distance, queue);
                    }
                }
                else
                {
                    // start right
                    (newNode, newDist) = FindNearestNeighbours(node.RightChild, pivot, k, pivotPoint, distance, queue);

                    if (!double.IsNaN(newDist) && newDist <= currentDistance && !newNode.Element.Equals(pivot))
                    {
                        queue.Add(newDist, newNode);
                        currentDistance = queue.LastDistance;
                        currentNearestNode = queue.LastElement;
                    }

                    if (node.LeftChild != null && pointValue - currentDistance <= node.L)
                    {
                        (newNode, newDist) = FindNearestNeighbours(node.LeftChild, pivot, k, pivotPoint, distance, queue);
                    }
                }

                if (!double.IsNaN(newDist) && newDist <= currentDistance && !newNode.Element.Equals(pivot))
                {
                    queue.Add(newDist, newNode);
                    currentDistance = queue.LastDistance;
                    currentNearestNode = queue.LastElement;
                }

                return (currentNearestNode, currentDistance);
            }
        }

        private class KNearestNeighboursQueue : SortedList<double, HashSet<KdTreeNode<T>>>
        {
            public readonly int K;

            public KdTreeNode<T> LastElement { get; private set; }

            public double LastDistance { get; private set; }

            public bool IsFull => Count >= K;

            public KNearestNeighboursQueue(int k) : base(k)
            {
                K = k;
                LastDistance = double.PositiveInfinity;
            }

            public void Add(double key, KdTreeNode<T> value)
            {
                if (key > LastDistance && IsFull)
                {
                    return;
                }

                if (!ContainsKey(key))
                {
                    base.Add(key, new HashSet<KdTreeNode<T>>());
                    if (Count > K)
                    {
                        RemoveAt(Count - 1);
                    }
                }

                if (this[key].Add(value))
                {
                    LastDistance = Keys[Count - 1];
                    var lastSet = Values[Count - 1];
                    KdTreeNode<T> lastElement = null;
                    foreach (var e in lastSet)
                    {
                        lastElement = e;
                    }
                    LastElement = lastElement;
                }
            }
        }
        #endregion

        internal readonly struct KdTreeElement<R>
        {
            internal KdTreeElement(int index, PdfPoint point, R value)
            {
                Index = index;
                Value = point;
                Element = value;
            }

            public int Index { get; }

            public PdfPoint Value { get; }

            public R Element { get; }
        }
        
        /// <summary>
        /// K-D tree leaf.
        /// </summary>
        /// <typeparam name="Q"></typeparam>
        public class KdTreeLeaf<Q> : KdTreeNode<Q>
        {
            /// <summary>
            /// Return true if leaf.
            /// </summary>
            public override bool IsLeaf => true;

            internal KdTreeLeaf(KdTreeElement<Q> point, int depth)
                : base(null, null, point, depth)
            { }

            /// <inheritdoc />
            public override string ToString()
            {
                return "Leaf->" + Value.ToString();
            }
        }

        /// <summary>
        /// K-D tree node.
        /// </summary>
        /// <typeparam name="Q"></typeparam>
        public class KdTreeNode<Q>
        {
            /// <summary>
            /// Split value (X or Y axis).
            /// </summary>
            public double L => IsAxisCutX ? Value.X : Value.Y;

            /// <summary>
            /// Split point.
            /// </summary>
            public PdfPoint Value { get; }

            /// <summary>
            /// Left child.
            /// </summary>
            public KdTreeNode<Q> LeftChild { get; internal set; }

            /// <summary>
            /// Right child.
            /// </summary>
            public KdTreeNode<Q> RightChild { get; internal set; }

            /// <summary>
            /// The node's element.
            /// </summary>
            public Q Element { get; }

            /// <summary>
            /// True if this cuts with X axis, false if cuts with Y axis.
            /// </summary>
            public bool IsAxisCutX { get; }

            /// <summary>
            /// The element's depth in the tree.
            /// </summary>
            public int Depth { get; }

            /// <summary>
            /// Return true if leaf.
            /// </summary>
            public virtual bool IsLeaf => false;

            /// <summary>
            /// The index of the element in the original array.
            /// </summary>
            public int Index { get; }

            internal KdTreeNode(KdTreeNode<Q> leftChild, KdTreeNode<Q> rightChild, KdTreeElement<Q> point, int depth)
            {
                LeftChild = leftChild;
                RightChild = rightChild;
                Value = point.Value;
                Element = point.Element;
                Depth = depth;
                IsAxisCutX = depth % 2 == 0;
                Index = point.Index;
            }

            /// <summary>
            /// Get the leaves.
            /// </summary>
            public IEnumerable<KdTreeLeaf<Q>> GetLeaves()
            {
                var leaves = new List<KdTreeLeaf<Q>>();
                RecursiveGetLeaves(LeftChild, ref leaves);
                RecursiveGetLeaves(RightChild, ref leaves);
                return leaves;
            }

            private void RecursiveGetLeaves(KdTreeNode<Q> leaf, ref List<KdTreeLeaf<Q>> leaves)
            {
                if (leaf == null)
                {
                    return;
                }

                if (leaf is KdTreeLeaf<Q> lLeaf)
                {
                    leaves.Add(lLeaf);
                }
                else
                {
                    RecursiveGetLeaves(leaf.LeftChild, ref leaves);
                    RecursiveGetLeaves(leaf.RightChild, ref leaves);
                }
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return "Node->" + Value.ToString();
            }
        }
    }
}
