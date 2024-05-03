namespace UglyToad.PdfPig.Outline
{
    /// <summary>
    /// The bookmarks in a PDF document.
    /// </summary>
    public class Bookmarks
    {
        /// <summary>
        /// The roots of the bookmarks' node tree.
        /// </summary>
        public IReadOnlyList<BookmarkNode> Roots { get; }

        /// <summary>
        /// Create a new <see cref="Bookmarks" />.
        /// </summary>
        public Bookmarks(IReadOnlyList<BookmarkNode> roots)
        {
            Roots = roots;
        }
        
        /// <summary>
        /// Get all nodes.
        /// </summary>
        public IEnumerable<BookmarkNode> GetNodes()
        {
            foreach (var root in Roots)
            {
                foreach (var child in GetNodes(root))
                {
                    yield return child;
                }
            }
        }

        private static IEnumerable<BookmarkNode> GetNodes(BookmarkNode node)
        {
            yield return node;

            foreach (var child in node.Children)
            {
                foreach (var childNode in GetNodes(child))
                {
                    yield return childNode;
                }
            }
        }
    }
}
