using System.Collections.Generic;

namespace UglyToad.PdfPig.Outline
{
    /// <summary>
    /// The bookmarks in a PDF document.
    /// </summary>
    public class Bookmarks
    {
        internal Bookmarks(IReadOnlyList<BookmarkNode> tree)
        {
            Tree = tree;
        }

        /// <summary>
        /// The bookmarks' node tree.
        /// </summary>
        public IReadOnlyList<BookmarkNode> Tree { get; }

        /// <summary>
        /// Get all nodes as a list.
        /// </summary>
        public IReadOnlyList<BookmarkNode> GetNodes()
        {
            List<BookmarkNode> nodes = new List<BookmarkNode>();
            GetNodes(Tree, nodes);
            return nodes;
        }

        private static void GetNodes(IReadOnlyList<BookmarkNode> roots, List<BookmarkNode> nodes)
        {
            foreach (var node in roots)
            {
                nodes.Add(node);
                if (node.Children.Count > 0)
                {
                    GetNodes(node.Children, nodes);
                }
            }
        }
    }
}
