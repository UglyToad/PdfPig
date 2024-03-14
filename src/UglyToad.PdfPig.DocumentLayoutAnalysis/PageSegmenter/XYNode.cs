namespace UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter
{
    using Core;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A Node used in the <see cref="RecursiveXYCut"/> algorithm.
    /// </summary>
    internal class XYNode
    {
        /// <summary>
        /// Returns true if this node is a leaf, false otherwise.
        /// </summary>
        public virtual bool IsLeaf => false;

        /// <summary>
        /// The rectangle completely containing the node.
        /// </summary>
        public PdfRectangle BoundingBox { get; set; }

        /// <summary>
        /// The children of the node.
        /// </summary>
        public XYNode[] Children { get; set; }

        /// <summary>
        /// Create a new <see cref="XYNode"/>.
        /// </summary>
        /// <param name="children">The node's children.</param>
        public XYNode(params XYNode[] children)
            : this(children?.ToList())
        {
        }

        /// <summary>
        /// Create a new <see cref="XYNode"/>.
        /// </summary>
        /// <param name="children">The node's children.</param>
        public XYNode(IEnumerable<XYNode> children)
        {
            if (children?.Any() == true)
            {
                Children = children.ToArray();
                BoundingBox = new PdfRectangle(children.Min(b => b.BoundingBox.Left),
                                               children.Min(b => b.BoundingBox.Bottom),
                                               children.Max(b => b.BoundingBox.Right),
                                               children.Max(b => b.BoundingBox.Top));
            }
            else
            {
                Children = [];
            }
        }

        /// <summary>
        /// Recursively counts the words included in this node.
        /// </summary>
        public virtual int CountWords()
        {
            if (Children == null)
            {
                return 0;
            }

            int count = 0;
            RecursiveCount(Children, ref count);
            return count;
        }

        /// <summary>
        /// Recursively gets the leaves (last nodes) of this node.
        /// </summary>
        public virtual List<XYLeaf> GetLeaves()
        {
            List<XYLeaf> leaves = new List<XYLeaf>();
            if (Children == null || Children.Length == 0)
            {
                return leaves;
            }

            int level = 0;
            RecursiveGetLeaves(Children, ref leaves, level);
            return leaves;
        }

        private void RecursiveCount(IEnumerable<XYNode> children, ref int count)
        {
            if (!children.Any())
            {
                return;
            }

            foreach (XYNode node in children.Where(x => x.IsLeaf))
            {
                count += node.CountWords();
            }

            foreach (XYNode node in children.Where(x => !x.IsLeaf))
            {
                RecursiveCount(node.Children, ref count);
            }
        }

        private void RecursiveGetLeaves(IEnumerable<XYNode> children, ref List<XYLeaf> leaves, int level)
        {
            if (!children.Any())
            {
                return;
            }

            bool isVerticalCut = level % 2 == 0;

            foreach (XYLeaf node in children.Where(x => x.IsLeaf))
            {
                leaves.Add(node);
            }

            level++;

            IEnumerable<XYNode> notLeaves = children.Where(x => !x.IsLeaf);

            if (isVerticalCut)
            {
                notLeaves = notLeaves.OrderBy(x => x.BoundingBox.Left).ToList();
            }
            else
            {
                notLeaves = notLeaves.OrderByDescending(x => x.BoundingBox.Top).ToList();
            }

            foreach (XYNode node in notLeaves)
            {
                RecursiveGetLeaves(node.Children, ref leaves, level);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return IsLeaf ? "Leaf" : "Node";
        }
    }
}
