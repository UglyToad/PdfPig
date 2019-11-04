using System.Collections.Generic;
using UglyToad.PdfPig.Geometry;

namespace UglyToad.PdfPig.Outline
{
    /// <summary>
    /// A node in the <see cref="Bookmarks"/> of a PDF document.
    /// </summary>
    public class BookmarkNode
    {
        /// <summary>
        /// The text displayed for this node.
        /// </summary>
        public string Title { get; internal set; }

        /// <summary>
        /// The bookmark's coordinates in the pdf page.
        /// </summary>
        public PdfPoint TopLeft { get; internal set; }

        /// <summary>
        /// The bookmark's bounding box in the pdf page.
        /// </summary>
        public PdfRectangle BoundingBox { get; internal set; }

        /// <summary>
        /// The node's hierarchical level.
        /// </summary>
        public int Level { get; internal set; }

        /// <summary>
        /// The page's number where the bookmark is located.
        /// </summary>
        public int PageNumber { get; internal set; }

        /// <summary>
        /// The link to an external source.
        /// </summary>
        public string ExternalLink { get; internal set; }

        /// <summary>
        /// True if bookmark refers to an external source.
        /// </summary>
        public bool IsExternal { get; internal set; }

        /// <summary>
        /// The bookmark's sub-bookmark.
        /// </summary>
        public List<BookmarkNode> Children { get; internal set; }

        /// <summary>
        /// Create a new instance of BookmarkNode.
        /// </summary>
        public BookmarkNode()
        {
            Title = string.Empty;
            Children = new List<BookmarkNode>();
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return "page #" + PageNumber + ", " + Level + ", " + Title;
        }
    }
}
