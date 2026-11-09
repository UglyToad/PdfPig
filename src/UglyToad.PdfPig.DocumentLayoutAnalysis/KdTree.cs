namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;
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

            Root = BuildTree(array, 0, Count, 0);
        }

        /// <summary>
        /// Build the tree from <c>elements[start..end)</c>, split on the median along X (even depth) or Y (odd depth).
        /// <para>Only the median needs to be in place, not the whole range sorted, so the elements are partitioned
        /// with a quickselect. Ties are broken by index, so the tree is the same as if the range was sorted.</para>
        /// </summary>
        private static KdTreeNode<T> BuildTree(KdTreeElement<T>[] elements, int start, int end, int depth)
        {
            int count = end - start;
            if (count == 0)
            {
                return null;
            }

            if (count == 1)
            {
                return new KdTreeLeaf<T>(elements[start], depth);
            }

            bool byX = depth % 2 == 0;

            if (count == 2)
            {
                if (Compare(elements[start + 1], elements[start], byX) < 0)
                {
                    Swap(elements, start, start + 1);
                }

                return new KdTreeNode<T>(new KdTreeLeaf<T>(elements[start], depth + 1), null, elements[start + 1], depth);
            }

            int median = start + count / 2;
            Select(elements, start, end - 1, median, byX);

            KdTreeNode<T> vLeft = BuildTree(elements, start, median, depth + 1);
            KdTreeNode<T> vRight = BuildTree(elements, median + 1, end, depth + 1);

            return new KdTreeNode<T>(vLeft, vRight, elements[median], depth);
        }

        /// <summary>
        /// Partition <c>elements[left..right]</c> (inclusive) so that the element at <paramref name="k"/> is the one
        /// that would be there if the range was sorted, with smaller elements before it and larger ones after it.
        /// <para>Quickselect with a median of three pivot, falling back to sorting the range if it does not converge.</para>
        /// </summary>
        private static void Select(KdTreeElement<T>[] elements, int left, int right, int k, bool byX)
        {
            // Each partition should roughly halve the range, allow for twice as many before giving up
            int maxIterations = 2 * (int)Math.Ceiling(Math.Log(right - left + 1, 2)) + 2;

            while (right > left)
            {
                if (maxIterations-- == 0)
                {
                    Array.Sort(elements, left, right - left + 1, byX ? KdTreeElementComparer.X : KdTreeElementComparer.Y);
                    return;
                }

                // Median of three, also ordering the first, middle and last elements
                int middle = left + (right - left) / 2;
                if (Compare(elements[middle], elements[left], byX) < 0)
                {
                    Swap(elements, left, middle);
                }

                if (Compare(elements[right], elements[left], byX) < 0)
                {
                    Swap(elements, left, right);
                }

                if (Compare(elements[right], elements[middle], byX) < 0)
                {
                    Swap(elements, middle, right);
                }

                var pivot = elements[middle];

                // Hoare partition: elements[left..j] <= pivot <= elements[i..right]
                int i = left;
                int j = right;
                while (i <= j)
                {
                    while (Compare(elements[i], pivot, byX) < 0)
                    {
                        i++;
                    }

                    while (Compare(elements[j], pivot, byX) > 0)
                    {
                        j--;
                    }

                    if (i <= j)
                    {
                        Swap(elements, i, j);
                        i++;
                        j--;
                    }
                }

                if (k <= j)
                {
                    right = j;
                }
                else if (k >= i)
                {
                    left = i;
                }
                else
                {
                    // j < k < i: elements[k] is the pivot
                    return;
                }
            }
        }

        private static int Compare(in KdTreeElement<T> p0, in KdTreeElement<T> p1, bool byX)
        {
            int comparison = byX ? p0.Value.X.CompareTo(p1.Value.X) : p0.Value.Y.CompareTo(p1.Value.Y);
            return comparison != 0 ? comparison : p0.Index.CompareTo(p1.Index);
        }

        private static void Swap(KdTreeElement<T>[] elements, int i, int j)
        {
            (elements[i], elements[j]) = (elements[j], elements[i]);
        }

        private sealed class KdTreeElementComparer : IComparer<KdTreeElement<T>>
        {
            public static readonly KdTreeElementComparer X = new KdTreeElementComparer(true);
            public static readonly KdTreeElementComparer Y = new KdTreeElementComparer(false);

            private readonly bool byX;

            private KdTreeElementComparer(bool byX)
            {
                this.byX = byX;
            }

            public int Compare(KdTreeElement<T> p0, KdTreeElement<T> p1)
            {
                return KdTree<T>.Compare(p0, p1, byX);
            }
        }

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

            KdTreeNode<T> nearest = null;
            double nearestDistance = double.PositiveInfinity;
            FindNearestNeighbour(Root, pivot, pivotPoint, distanceMeasure, ref nearest, ref nearestDistance);

            if (nearest is null)
            {
                index = -1;
                distance = double.NaN;
                return default;
            }

            index = nearest.Index;
            distance = nearestDistance;
            return nearest.Element;
        }

        /// <summary>
        /// Depth-first search visiting the node, then the child on the pivot's side, then the other child if it can
        /// contain a point as near as the nearest found so far. A point at the same distance replaces the nearest
        /// found so far, i.e. the last visited wins.
        /// </summary>
        private static void FindNearestNeighbour(KdTreeNode<T> node,
            T pivot,
            PdfPoint pivotPoint,
            Func<PdfPoint, PdfPoint, double> distance,
            ref KdTreeNode<T> nearest,
            ref double nearestDistance)
        {
            while (true)
            {
                // The pivot is not a candidate, otherwise it could be returned as its own neighbour
                if (!EqualityComparer<T>.Default.Equals(node.Element, pivot))
                {
                    double nodeDistance = distance(node.Value, pivotPoint);
                    if (nodeDistance <= nearestDistance)
                    {
                        nearest = node;
                        nearestDistance = nodeDistance;
                    }
                }

                var pointValue = node.IsAxisCutX ? pivotPoint.X : pivotPoint.Y;
                var split = node.L;

                if (pointValue < split)
                {
                    // start left
                    if (node.LeftChild != null)
                    {
                        FindNearestNeighbour(node.LeftChild, pivot, pivotPoint, distance, ref nearest, ref nearestDistance);
                    }

                    if (node.RightChild != null && pointValue + nearestDistance >= split)
                    {
                        node = node.RightChild;
                        continue;
                    }
                }
                else
                {
                    // start right
                    if (node.RightChild != null)
                    {
                        FindNearestNeighbour(node.RightChild, pivot, pivotPoint, distance, ref nearest, ref nearestDistance);
                    }

                    if (node.LeftChild != null && pointValue - nearestDistance <= split)
                    {
                        node = node.LeftChild;
                        continue;
                    }
                }

                break;
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
            FindNearestNeighbours(Root, pivot, pivotPoint, distanceMeasure, kdTreeNodes);

            var results = new List<(T, int, double)>(kdTreeNodes.Count);
            for (int i = 0; i < kdTreeNodes.Count; i++)
            {
                var (distance, node) = kdTreeNodes[i];
                results.Add((node.Element, node.Index, distance));
            }
            return results;
        }

        /// <summary>
        /// Depth-first search visiting the node, then the child on the pivot's side, then the other child unless the
        /// splitting plane is farther than the largest distance held (and K distinct distances are held already).
        /// A node at exactly that distance is held too, hence the plane being allowed to be as far as it.
        /// </summary>
        private static void FindNearestNeighbours(KdTreeNode<T> node,
            T pivot,
            PdfPoint pivotPoint,
            Func<PdfPoint, PdfPoint, double> distance,
            KNearestNeighboursQueue queue)
        {
            while (true)
            {
                // The pivot is not a candidate, otherwise it could be returned as its own neighbour
                if (!EqualityComparer<T>.Default.Equals(node.Element, pivot))
                {
                    queue.Add(distance(node.Value, pivotPoint), node);
                }

                double pointValue = node.IsAxisCutX ? pivotPoint.X : pivotPoint.Y;
                bool startLeft = pointValue < node.L;
                KdTreeNode<T> near = startLeft ? node.LeftChild : node.RightChild;
                KdTreeNode<T> far = startLeft ? node.RightChild : node.LeftChild;

                if (near is not null)
                {
                    FindNearestNeighbours(near, pivot, pivotPoint, distance, queue);
                }

                if (far is not null)
                {
                    // A relative margin keeps a node whose distance equals the largest held one, up to rounding in the
                    // distance measure, from being cut off.
                    if (!queue.IsFull || Math.Abs(pointValue - node.L) <= queue.LastDistance * (1.0 + 1e-9))
                    {
                        node = far;
                        continue;
                    }
                }

                break;
            }
        }

        /// <summary>
        /// The nodes found so far, nearest first, holding every node at each of the <c>K</c> smallest distinct
        /// distances (so more than K nodes when points are equidistant). Nodes at one distance are kept in the order
        /// they were added, and a node is only kept once per distance.
        /// <para>One flat sorted array, not a sorted list of sets, so a query does not allocate a hash set per distinct distance.</para>
        /// </summary>
        private sealed class KNearestNeighboursQueue
        {
            private readonly int K;
            private (double Distance, KdTreeNode<T> Node)[] _entries;
            private int _distinctDistances;

            /// <summary>Number of nodes held, which can exceed K.</summary>
            public int Count { get; private set; }

            /// <summary>The largest distance held, <see cref="double.PositiveInfinity"/> while empty.</summary>
            public double LastDistance { get; private set; }

            /// <summary>True once K distinct distances are held.</summary>
            public bool IsFull => _distinctDistances >= K;

            public (double Distance, KdTreeNode<T> Node) this[int index] => _entries[index];

            public KNearestNeighboursQueue(int k)
            {
                K = k;
                _entries = new (double, KdTreeNode<T>)[Math.Max(k, 0) + 1];
                LastDistance = double.PositiveInfinity;
            }

            public void Add(double distance, KdTreeNode<T> node)
            {
                if (distance > LastDistance && IsFull)
                {
                    return;
                }

                // Insert after the nodes at the same distance, so ties stay in the order they were added
                int position = Count;
                bool distanceHeld = false;
                for (int i = 0; i < Count; i++)
                {
                    int comparison = _entries[i].Distance.CompareTo(distance);
                    if (comparison == 0)
                    {
                        if (ReferenceEquals(_entries[i].Node, node))
                        {
                            return;
                        }

                        distanceHeld = true;
                    }
                    else if (comparison > 0)
                    {
                        position = i;
                        break;
                    }
                }

                if (Count == _entries.Length)
                {
                    Array.Resize(ref _entries, Count * 2);
                }

                Array.Copy(_entries, position, _entries, position + 1, Count - position);
                _entries[position] = (distance, node);
                Count++;

                if (!distanceHeld && ++_distinctDistances > K)
                {
                    // Over K distinct distances: drop every node at the largest one
                    double largest = _entries[Count - 1].Distance;
                    while (Count > 0 && _entries[Count - 1].Distance.CompareTo(largest) == 0)
                    {
                        _entries[--Count] = default;
                    }

                    _distinctDistances--;
                }

                LastDistance = _entries[Count - 1].Distance;
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
