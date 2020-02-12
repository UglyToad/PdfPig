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
            var pointsIndex = Enumerable.Range(0, candidates.Count).Zip(candidates, (e, p) => (e, candidatesPointFunc(p), p)).ToList();
            if (candidates != null && candidates.Count > 0)
            {
                Root = BuildTree(pointsIndex, 0);
            }
        }

        private KdTreeNode<T> BuildTree(IReadOnlyList<(int, PdfPoint, T)> P, int depth)
        {
            var median = P.Count / 2;
            if (depth % 2 == 0) // depth is even
            {
                P = P.OrderBy(p => p.Item2.X).ToArray();
            }
            else
            {
                P = P.OrderBy(p => p.Item2.Y).ToArray();
            }

            // left side
            var P1 = P.Take(median).ToArray();
            KdTreeNode<T> vLeft = null;
            if (P1.Length == 1)
            {
                var item = P1[0];
                vLeft = new KdTreeLeaf<T>(item.Item2, item.Item3, depth, item.Item1);
            }
            else if (P1.Length > 1)
            {
                vLeft = BuildTree(P1, depth + 1);
            }

            // right side
            var P2 = P.Skip(median + 1).ToArray();
            KdTreeNode<T> vRight = null;
            if (P2.Length == 1)
            {
                var item = P2[0];
                vRight = new KdTreeLeaf<T>(item.Item2, item.Item3, depth, item.Item1);
            }
            else if (P2.Length > 1)
            {
                vRight = BuildTree(P2, depth + 1);
            }

            var medianItem = P[median];
            return new KdTreeNode<T>(vLeft, vRight, medianItem.Item2, medianItem.Item3, depth, medianItem.Item1);
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

            public KdTreeLeaf(PdfPoint l, Q element, int depth, int index)
                : base(null, null, l, element, depth, index)
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

            public KdTreeNode(KdTreeNode<Q> leftChild, KdTreeNode<Q> rightChild, PdfPoint l, Q element, int depth, int index)
            {
                LeftChild = leftChild;
                RightChild = rightChild;
                Value = l;
                Element = element;
                Depth = depth % 2;
                Index = index;
            }

            public IEnumerable<KdTreeLeaf<Q>> GetLeaves()
            {
                var leafs = new List<KdTreeLeaf<Q>>();
                RecursiveGetLeaves(LeftChild, ref leafs);
                RecursiveGetLeaves(RightChild, ref leafs);
                return leafs;
            }

            private void RecursiveGetLeaves(KdTreeNode<Q> leaf, ref List<KdTreeLeaf<Q>> leafs)
            {
                if (leaf == null) return;
                if (leaf is KdTreeLeaf<Q> lLeaf)
                {
                    leafs.Add(lLeaf);
                }
                else
                {
                    RecursiveGetLeaves(leaf.LeftChild, ref leafs);
                    RecursiveGetLeaves(leaf.RightChild, ref leafs);
                }
            }

            public override string ToString()
            {
                return "Node->" + Value.ToString();
            }
        }
    }
}
