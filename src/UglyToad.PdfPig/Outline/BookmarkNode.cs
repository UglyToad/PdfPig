using System.Collections.Generic;
using UglyToad.PdfPig.Geometry;

namespace UglyToad.PdfPig.Outline
{
    using System;

    /// <summary>
    /// A node in the <see cref="Bookmarks"/> of a PDF document.
    /// </summary>
    public class BookmarkNode
    {
        /// <summary>
        /// The text displayed for this node.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The bookmark's coordinates in the pdf page.
        /// </summary>
        public PdfPoint TopLeft { get; }

        /// <summary>
        /// The bookmark's bounding box in the pdf page.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// The node's hierarchical level.
        /// </summary>
        public int Level { get; }

        /// <summary>
        /// The page number where the bookmark is located.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The link to an external source.
        /// </summary>
        public string ExternalLink { get; }

        /// <summary>
        /// True if bookmark refers to an external source.
        /// </summary>
        public bool IsExternal { get; }

        /// <summary>
        /// The bookmark's sub-bookmark.
        /// </summary>
        public IReadOnlyList<BookmarkNode> Children { get; }

        /// <inheritdoc />
        public BookmarkNode(string title, PdfPoint topLeft, PdfRectangle boundingBox, int level, int pageNumber, 
            string externalLink, 
            bool isExternal,
            IReadOnlyList<BookmarkNode> children)
        {
            Title = title;
            TopLeft = topLeft;
            BoundingBox = boundingBox;
            Level = level;
            PageNumber = pageNumber;
            ExternalLink = externalLink;
            IsExternal = isExternal;
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "page #" + PageNumber + ", " + Level + ", " + Title;
        }
    }
}
