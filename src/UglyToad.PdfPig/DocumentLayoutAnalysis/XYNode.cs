using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Util;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// A Node used in the <see cref="RecursiveXYCut"/> algorithm.
    /// </summary>
    public class XYNode
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
            if (children != null && children.Count() != 0)
            {
                Children = children.ToArray();
                decimal left = children.Min(b => b.BoundingBox.Left);
                decimal right = children.Max(b => b.BoundingBox.Right);
                decimal bottom = children.Min(b => b.BoundingBox.Bottom);
                decimal top = children.Max(b => b.BoundingBox.Top);
                BoundingBox = new PdfRectangle(left, bottom, right, top);
            }
            else
            {
                Children = EmptyArray<XYNode>.Instance;
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
        /// Recursively gets the leafs (last nodes) of this node.
        /// </summary>
        public virtual List<XYLeaf> GetLeafs()
        {
            List<XYLeaf> leafs = new List<XYLeaf>();
            if (Children == null || Children.Length == 0)
            {
                return leafs;
            }

            int level = 0;
            RecursiveGetLeafs(Children, ref leafs, level);
            return leafs;
        }

        private void RecursiveCount(IEnumerable<XYNode> children, ref int count)
        {
            if (children.Count() == 0) return;
            foreach (XYNode node in children.Where(x => x.IsLeaf))
            {
                count += node.CountWords();
            }

            foreach (XYNode node in children.Where(x => !x.IsLeaf))
            {
                RecursiveCount(node.Children, ref count);
            }
        }

        private void RecursiveGetLeafs(IEnumerable<XYNode> children, ref List<XYLeaf> leafs, int level)
        {
            if (children.Count() == 0) return;
            bool isVerticalCut = level % 2 == 0;

            foreach (XYLeaf node in children.Where(x => x.IsLeaf))
            {
                leafs.Add(node);
            }

            level++;

            IEnumerable<XYNode> notLeafs = children.Where(x => !x.IsLeaf);

            if (isVerticalCut)
            {
                notLeafs = notLeafs.OrderBy(x => x.BoundingBox.Left).ToList();
            }
            else
            {
                notLeafs = notLeafs.OrderByDescending(x => x.BoundingBox.Top).ToList();
            }

            foreach (XYNode node in notLeafs)
            {
                RecursiveGetLeafs(node.Children, ref leafs, level);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return IsLeaf ? "Leaf" : "Node";
        }
    }
}
