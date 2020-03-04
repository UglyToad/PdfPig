namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Core;

    // for kd-tree with line segments, see https://stackoverflow.com/questions/14376679/how-to-represent-line-segments-in-kd-tree 

    internal class KdTree : KdTree<PdfPoint>
    {
        public KdTree(PdfPoint[] candidates) : base(candidates, p => p)
        { }

        public PdfPoint FindNearestNeighbours(PdfPoint pivot, Func<PdfPoint, PdfPoint, double> distanceMeasure, out int index, out double distance)
        {
            return FindNearestNeighbours(pivot, p => p, distanceMeasure, out index, out distance);
        }
    }

    internal class KdTree<T>
    {
        private KdTreeNode<T> Root;

        public KdTree(IReadOnlyList<T> candidates, Func<T, PdfPoint> candidatesPointFunc)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new ArgumentException("KdTree(): candidates cannot be null or empty.", nameof(candidates));
            }

            Root = BuildTree(Enumerable.Range(0, candidates.Count).Zip(candidates, (e, p) => (e, candidatesPointFunc(p), p)).ToArray(), 0);
        }

        private KdTreeNode<T> BuildTree((int, PdfPoint, T)[] P, int depth)
        {
            if (P.Length == 0)
            {
                return null;
            }
            else if (P.Length == 1)
            {
                return new KdTreeLeaf<T>(P[0], depth);
            }

            if (depth % 2 == 0)
            {
                Array.Sort(P, (p0, p1) => p0.Item2.X.CompareTo(p1.Item2.X));
            }
            else
            {
                Array.Sort(P, (p0, p1) => p0.Item2.Y.CompareTo(p1.Item2.Y));
            }

            if (P.Length == 2)
            {
                return new KdTreeNode<T>(new KdTreeLeaf<T>(P[0], depth), null, P[1], depth);
            }

            int median = P.Length / 2;

            KdTreeNode<T> vLeft = BuildTree(P.Take(median).ToArray(), depth + 1);
            KdTreeNode<T> vRight = BuildTree(P.Skip(median + 1).ToArray(), depth + 1);

            return new KdTreeNode<T>(vLeft, vRight, P[median], depth);
        }

        #region NN
        public T FindNearestNeighbours(T pivot, Func<T, PdfPoint> pivotPointFunc, Func<PdfPoint, PdfPoint, double> distanceMeasure, out int index, out double distance)
        {
            var result = FindNearestNeighbours(Root, pivot, pivotPointFunc, distanceMeasure);
            index = result.Item1.Index;
            distance = result.Item2.Value;
            return result.Item1.Element;
        }

        private static (KdTreeNode<T>, double?) FindNearestNeighbours(KdTreeNode<T> node, T pivot, Func<T, PdfPoint> pivotPointFunc, Func<PdfPoint, PdfPoint, double> distance)
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
                return (node, distance(node.Value, pivotPointFunc(pivot)));
            }
            else
            {
                var point = pivotPointFunc(pivot);
                var currentNearestNode = node;
                var currentDistance = distance(node.Value, point);

                KdTreeNode<T> newNode = null;
                double? newDist = null;

                var pointValue = node.Depth == 0 ? point.X : point.Y;

                if (pointValue < node.L)
                {
                    // start left
                    (newNode, newDist) = FindNearestNeighbours(node.LeftChild, pivot, pivotPointFunc, distance);

                    if (newDist.HasValue && newDist <= currentDistance && !newNode.Element.Equals(pivot))
                    {
                        currentDistance = newDist.Value;
                        currentNearestNode = newNode;
                    }

                    if (node.RightChild != null && pointValue + currentDistance >= node.L)
                    {
                        (newNode, newDist) = FindNearestNeighbours(node.RightChild, pivot, pivotPointFunc, distance);
                    }
                }
                else
                {
                    // start right
                    (newNode, newDist) = FindNearestNeighbours(node.RightChild, pivot, pivotPointFunc, distance);

                    if (newDist.HasValue && newDist <= currentDistance && !newNode.Element.Equals(pivot))
                    {
                        currentDistance = newDist.Value;
                        currentNearestNode = newNode;
                    }

                    if (node.LeftChild != null && pointValue - currentDistance <= node.L)
                    {
                        (newNode, newDist) = FindNearestNeighbours(node.LeftChild, pivot, pivotPointFunc, distance);
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

        private class KdTreeLeaf<Q> : KdTreeNode<Q>
        {
            public override bool IsLeaf => true;

            public KdTreeLeaf((int, PdfPoint, Q) point, int depth)
                : base(null, null, point, depth)
            { }

            public override string ToString()
            {
                return "Leaf->" + Value.ToString();
            }
        }

        private class KdTreeNode<Q>
        {
            /// <summary>
            /// Split value.
            /// </summary>
            public double L => Depth == 0 ? Value.X : Value.Y;

            public PdfPoint Value { get; }

            public KdTreeNode<Q> LeftChild { get; internal set; }

            public KdTreeNode<Q> RightChild { get; internal set; }

            public Q Element { get; }

            /// <summary>
            /// 0 is even (x), 1 is odd (y).
            /// </summary>
            public int Depth { get; }

            public virtual bool IsLeaf => false;

            public int Index { get; }

            public KdTreeNode(KdTreeNode<Q> leftChild, KdTreeNode<Q> rightChild, (int, PdfPoint, Q) point, int depth)
            {
                LeftChild = leftChild;
                RightChild = rightChild;
                Value = point.Item2;
                Element = point.Item3;
                Depth = depth % 2;
                Index = point.Item1;
            }

            public IEnumerable<KdTreeLeaf<Q>> GetLeaves()
            {
                var leaves = new List<KdTreeLeaf<Q>>();
                RecursiveGetLeaves(LeftChild, ref leaves);
                RecursiveGetLeaves(RightChild, ref leaves);
                return leaves;
            }

            private void RecursiveGetLeaves(KdTreeNode<Q> leaf, ref List<KdTreeLeaf<Q>> leaves)
            {
                if (leaf == null) return;
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

            public override string ToString()
            {
                return "Node->" + Value.ToString();
            }
        }
    }
}
